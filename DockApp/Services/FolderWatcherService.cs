using System.IO;
using System.Windows.Threading;

namespace DockApp.Services;

/// <summary>
/// Monitora uma pasta do sistema de arquivos em busca de alterações.
///
/// O serviço utiliza <see cref="FileSystemWatcher"/> para detectar
/// arquivos criados, excluídos, renomeados ou modificados.
///
/// Como o Windows pode disparar vários eventos para uma única ação
/// do usuário, como copiar ou alterar um arquivo, o serviço utiliza
/// um mecanismo de debounce de 300 ms. Dessa forma, vários eventos
/// consecutivos são agrupados em uma única notificação através
/// do evento <see cref="OnChanged"/>.
///
/// Este serviço deve ser criado na thread da interface gráfica (UI),
/// pois utiliza <see cref="DispatcherTimer"/> para executar o debounce.
/// </summary>
public class FolderWatcherService : IDisposable
{
    /// <summary>
    /// Objeto responsável por monitorar alterações na pasta definida.
    ///
    /// O FileSystemWatcher recebe notificações do sistema operacional
    /// quando arquivos são criados, excluídos, modificados ou renomeados.
    /// </summary>
    private readonly FileSystemWatcher _watcher;

    /// <summary>
    /// Timer utilizado para aplicar o mecanismo de debounce.
    ///
    /// Quando vários eventos são recebidos em sequência, o timer é
    /// reiniciado a cada evento. O evento OnChanged só será disparado
    /// quando permanecerem 300 ms sem novas alterações.
    /// </summary>
    private readonly DispatcherTimer _debounceTimer;

    /// <summary>
    /// Evento disparado quando uma ou mais alterações na pasta
    /// forem detectadas e o período de debounce tiver terminado.
    ///
    /// O evento é opcional e pode não possuir nenhum assinante.
    /// </summary>
    public event Action? OnChanged;

    /// <summary>
    /// Inicializa uma nova instância do observador de pastas.
    ///
    /// Configura o FileSystemWatcher para monitorar apenas arquivos
    /// diretamente dentro da pasta informada, sem observar subpastas.
    /// Também configura o DispatcherTimer responsável pelo debounce.
    /// </summary>
    /// <param name="folder">
    /// Caminho da pasta que deverá ser monitorada.
    /// </param>
    /// <param name="dispatcher">
    /// Dispatcher da thread da interface gráfica (UI).
    /// É utilizado pelo DispatcherTimer para executar seus eventos
    /// na thread correta.
    /// </param>
    public FolderWatcherService(string folder, Dispatcher dispatcher)
    {
        // Cria o timer de debounce associado ao Dispatcher da UI.
        //
        // DispatcherPriority.Background faz com que a atualização
        // seja executada em uma prioridade baixa, evitando interferir
        // desnecessariamente nas operações mais importantes da interface.
        _debounceTimer = new DispatcherTimer(
            DispatcherPriority.Background,
            dispatcher)
        {
            // Aguarda 300 ms desde o último evento antes de considerar
            // que a sequência de alterações terminou.
            Interval = TimeSpan.FromMilliseconds(300)
        };

        // Evento executado quando o timer atingir o intervalo definido.
        _debounceTimer.Tick += (_, _) =>
        {
            // Para o timer para impedir que o mesmo evento seja
            // executado novamente automaticamente.
            _debounceTimer.Stop();

            // Dispara o evento OnChanged caso exista algum código
            // inscrito nele.
            OnChanged?.Invoke();
        };

        // Cria o observador para a pasta informada.
        _watcher = new FileSystemWatcher(folder)
        {
            // Define quais tipos de alteração devem ser monitorados.
            //
            // FileName:
            // Detecta criação, exclusão e renomeação de arquivos.
            //
            // LastWrite:
            // Detecta alterações no conteúdo ou data de modificação.
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,

            // Não monitora subpastas.
            //
            // Apenas os arquivos existentes diretamente dentro
            // da pasta configurada serão observados.
            IncludeSubdirectories = false,

            // O monitoramento começa desativado.
            //
            // Start() será responsável por ativá-lo posteriormente.
            EnableRaisingEvents = false
        };

        // Quando um novo arquivo for criado, agenda uma atualização.
        _watcher.Created += (_, _) => ScheduleReload();

        // Quando um arquivo for excluído, agenda uma atualização.
        _watcher.Deleted += (_, _) => ScheduleReload();

        // Quando um arquivo for renomeado, agenda uma atualização.
        _watcher.Renamed += (_, _) => ScheduleReload();

        // Quando um arquivo for alterado, agenda uma atualização.
        _watcher.Changed += (_, _) => ScheduleReload();
    }

    /// <summary>
    /// Inicia o monitoramento da pasta.
    ///
    /// A partir deste momento, o FileSystemWatcher passa a receber
    /// eventos de alterações do sistema de arquivos.
    /// </summary>
    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Interrompe temporariamente o monitoramento da pasta.
    ///
    /// O observador continua existindo, mas deixa de receber eventos
    /// até que o método <see cref="Start"/> seja chamado novamente.
    /// </summary>
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
    }

    /// <summary>
    /// Altera a pasta que está sendo monitorada.
    ///
    /// O monitoramento é temporariamente desativado durante a troca
    /// do caminho para evitar que eventos sejam processados enquanto
    /// a configuração está sendo alterada.
    /// </summary>
    /// <param name="folder">
    /// Novo caminho da pasta que deverá ser monitorada.
    /// </param>
    public void ChangeFolder(string folder)
    {
        // Interrompe temporariamente o monitoramento.
        _watcher.EnableRaisingEvents = false;

        // Altera o caminho da pasta monitorada.
        _watcher.Path = folder;

        // Reativa o monitoramento utilizando a nova pasta.
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Agenda uma atualização da Dock após uma alteração na pasta.
    ///
    /// O timer é reiniciado sempre que um novo evento chega.
    /// Portanto, se o Windows disparar vários eventos em sequência,
    /// o timer continuará sendo reiniciado.
    ///
    /// Exemplo:
    ///
    /// Evento 1 → inicia timer
    /// Evento 2 → reinicia timer
    /// Evento 3 → reinicia timer
    /// 300 ms sem novos eventos → OnChanged
    ///
    /// Isso evita que uma única operação gere várias atualizações
    /// desnecessárias da Dock.
    /// </summary>
    private void ScheduleReload()
    {
        // Interrompe o timer atual, caso ele esteja contando.
        _debounceTimer.Stop();

        // Inicia novamente a contagem de 300 ms.
        //
        // Se outro evento chegar antes dos 300 ms, o timer será
        // novamente interrompido e reiniciado.
        _debounceTimer.Start();
    }

    /// <summary>
    /// Libera os recursos utilizados pelo serviço.
    ///
    /// O FileSystemWatcher utiliza recursos do sistema operacional,
    /// portanto precisa ser descartado quando não for mais necessário.
    /// O timer também é interrompido para impedir que eventos
    /// continuem sendo processados.
    /// </summary>
    public void Dispose()
    {
        // Libera o FileSystemWatcher e suas associações
        // com o sistema operacional.
        _watcher.Dispose();

        // Garante que o timer não continue executando.
        _debounceTimer.Stop();
    }
}