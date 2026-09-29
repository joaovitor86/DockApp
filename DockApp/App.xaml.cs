// Importa recursos de diagnóstico, utilizados para registrar
// mensagens de depuração e o log de exceções não tratadas.
using System.Diagnostics;

// Importa recursos para manipulação de diretórios e arquivos,
// utilizados pelo log de exceções não tratadas.
using System.IO;

// Importa os recursos principais do WPF, incluindo:
// - Application
// - StartupEventArgs
// - ExitEventArgs
using System.Windows;

// Importa os componentes do Windows Forms.
//
// O DockApp utiliza o Windows Forms especificamente para o
// NotifyIcon e seu menu de contexto na bandeja do Windows.
using System.Windows.Forms;

// Existe uma classe Application tanto no WPF quanto no Windows Forms.
// Este alias garante que "Application" neste arquivo sempre se refere
// à classe Application do WPF.
using Application = System.Windows.Application;

namespace DockApp;

/// <summary>
/// Classe principal da aplicação WPF do DockApp.
///
/// Responsabilidades:
/// - Inicializar a aplicação.
/// - Registrar um handler global para exceções não tratadas,
///   evitando que uma falha pontual encerre o processo inteiro.
/// - Criar e exibir a janela principal.
/// - Configurar o ícone do aplicativo na bandeja do Windows.
/// - Disponibilizar ações através do menu da bandeja.
/// - Controlar o encerramento completo da aplicação.
///
/// O DockApp utiliza <see cref="ShutdownMode.OnExplicitShutdown"/>
/// para continuar executando mesmo quando a janela principal é ocultada.
/// Dessa forma, a Dock pode desaparecer da tela sem encerrar o processo.
///
/// O encerramento completo ocorre somente quando <see cref="Shutdown"/>
/// é chamado explicitamente pelo método <see cref="ExitApplication"/>.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Nome do arquivo de ícone utilizado tanto pelo ícone
    /// embutido no executável (ApplicationIcon, no .csproj)
    /// quanto pelo ícone exibido na bandeja do Windows.
    /// </summary>
    private const string TrayIconFileName =
        "ChatGPT-Image-26-de-set.-de-2026_-20_43_10.ico";

    /// <summary>
    /// Ícone do DockApp exibido na área de notificação
    /// (bandeja do Windows).
    ///
    /// A referência é nullable porque o NotifyIcon só é criado
    /// durante a inicialização da aplicação, em <see cref="SetupTrayIcon"/>.
    /// </summary>
    private NotifyIcon? _trayIcon;

    /// <summary>
    /// Referência para a janela principal do DockApp.
    ///
    /// A referência é nullable porque a janela ainda não existe
    /// durante as primeiras etapas da inicialização da aplicação.
    ///
    /// Essa referência é utilizada principalmente para:
    /// - Mostrar a Dock novamente.
    /// - Abrir a tela de configurações.
    /// </summary>
    private MainWindow? _mainWindow;

    /// <summary>
    /// Executado quando a aplicação WPF é iniciada.
    ///
    /// Responsabilidades da inicialização:
    /// 1. Registrar o handler global de exceções não tratadas.
    /// 2. Executar a inicialização padrão do WPF.
    /// 3. Configurar o encerramento explícito da aplicação.
    /// 4. Criar a janela principal.
    /// 5. Exibir a Dock.
    /// 6. Configurar o ícone da bandeja do Windows.
    /// </summary>
    /// <param name="e">
    /// Argumentos fornecidos pelo WPF durante o evento de inicialização.
    /// </param>
    protected override void OnStartup(StartupEventArgs e)
    {
        // Registra o handler global de exceções não tratadas antes
        // de qualquer outra coisa, para que ele já esteja ativo
        // durante toda a inicialização da aplicação.
        //
        // Sem esse handler, qualquer exceção não tratada em algum
        // ponto da inicialização ou do uso normal do DockApp (por
        // exemplo: extração de um ícone problemático, leitura de
        // um atalho corrompido, falha ao criar algum recurso do
        // Windows logo após o login) encerraria o processo inteiro
        // sem aviso — que é exatamente o sintoma de "a Dock aparece,
        // trava por alguns segundos e fecha sozinha".
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // Executa a inicialização padrão da classe Application.
        base.OnStartup(e);

        // Impede que a aplicação seja encerrada automaticamente
        // quando a janela principal for fechada ou ocultada.
        //
        // Isso é importante para o funcionamento de uma Dock:
        // a interface pode desaparecer temporariamente da tela,
        // mas o processo precisa continuar ativo em segundo plano.
        //
        // O encerramento será realizado explicitamente através
        // de Shutdown(), chamado no método ExitApplication().
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Cria a janela principal do DockApp.
        _mainWindow = new MainWindow();

        // Exibe a Dock na tela.
        _mainWindow.Show();

        // Cria e configura o ícone e o menu da bandeja do Windows.
        SetupTrayIcon();
    }

    /// <summary>
    /// Executado sempre que uma exceção não tratada chega até o
    /// Dispatcher da interface WPF (ou seja, qualquer exceção que
    /// nenhum try/catch mais específico conseguiu capturar antes).
    ///
    /// Em vez de deixar o WPF encerrar o processo inteiro, o erro
    /// é registrado em um arquivo de log (já que o DockApp, sendo
    /// um app de bandeja, normalmente não tem nenhuma janela de
    /// console visível para mostrar a mensagem), e a exceção é
    /// marcada como tratada, permitindo que a Dock continue
    /// funcionando normalmente.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Contém a exceção não tratada e a propriedade Handled,
    /// que controla se o processo deve ou não ser encerrado.
    /// </param>
    private void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        // Registra o erro para diagnóstico futuro.
        LogUnhandledException(e.Exception);

        // Marca a exceção como tratada para impedir que o WPF
        // encerre o processo inteiro por causa de um erro pontual.
        e.Handled = true;
    }

    /// <summary>
    /// Grava uma exceção não tratada em um arquivo de log, em:
    ///
    /// %AppData%\DockApp\crash.log
    ///
    /// A gravação em si também é protegida por try/catch: se nem
    /// o log puder ser escrito (por exemplo, por falta de espaço
    /// em disco ou permissão), o app simplesmente continua rodando
    /// mesmo assim — registrar o erro é útil, mas nunca deve ser
    /// motivo para uma nova falha.
    /// </summary>
    /// <param name="exception">
    /// Exceção não tratada que deve ser registrada.
    /// </param>
    private static void LogUnhandledException(Exception exception)
    {
        try
        {
            // Mesma pasta de configuração utilizada pelo
            // ConfigurationService, para manter tudo organizado
            // em um único lugar.
            var folder =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "DockApp");

            Directory.CreateDirectory(folder);

            var logPath = Path.Combine(folder, "crash.log");

            // Adiciona a nova entrada ao final do arquivo,
            // preservando o histórico de execuções anteriores.
            File.AppendAllText(
                logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                $"{exception}{Environment.NewLine}{Environment.NewLine}"
            );
        }
        catch
        {
            // Se nem o log puder ser gravado, não há mais nada
            // a fazer além de deixar o app continuar rodando.
        }
    }

    /// <summary>
    /// Cria e configura o ícone do DockApp na bandeja do Windows.
    ///
    /// Também cria o menu de contexto associado ao ícone,
    /// permitindo ao usuário acessar as principais funções
    /// do aplicativo sem precisar abrir a janela principal.
    /// </summary>
    private void SetupTrayIcon()
    {
        // Cria o menu de contexto da bandeja.
        //
        // ContextMenuStrip pertence ao Windows Forms e é utilizado
        // em conjunto com o NotifyIcon para criar o menu exibido
        // através do botão direito do mouse.
        var menu = new ContextMenuStrip();

        // Adiciona a opção "Mostrar Dock".
        //
        // Ao selecionar essa opção, solicita à janela principal
        // que torne a Dock visível novamente.
        //
        // O operador ?. impede uma NullReferenceException caso
        // _mainWindow ainda não esteja disponível.
        menu.Items.Add(
            "Mostrar Dock",
            null,
            (_, _) => _mainWindow?.ShowDock()
        );

        // Adiciona a opção "Configurações...".
        //
        // Permite acessar as configurações do DockApp diretamente
        // através do menu da bandeja.
        menu.Items.Add(
            "Configurações...",
            null,
            (_, _) => _mainWindow?.OpenSettingsFromTray()
        );

        // Adiciona uma linha separadora entre as opções
        // de gerenciamento e a opção de encerramento.
        menu.Items.Add(new ToolStripSeparator());

        // Adiciona a opção "Sair".
        //
        // Essa opção encerra completamente o DockApp.
        menu.Items.Add(
            "Sair",
            null,
            (_, _) => ExitApplication()
        );

        // Cria o ícone que será exibido na bandeja do Windows.
        _trayIcon = new NotifyIcon
        {
            // Carrega o ícone personalizado do DockApp.
            //
            // LoadTrayIcon() resolve o caminho de forma robusta
            // (independente do diretório de trabalho do processo)
            // e nunca lança exceção — em caso de falha, retorna
            // um ícone padrão do sistema. Ver LoadTrayIcon() para
            // detalhes.
            Icon = LoadTrayIcon(),

            // Torna o ícone visível na bandeja do Windows.
            Visible = true,

            // Texto exibido pelo Windows ao posicionar o mouse
            // sobre o ícone da bandeja.
            Text = "DockApp",

            // Associa o menu de contexto criado anteriormente
            // ao ícone da bandeja.
            ContextMenuStrip = menu
        };

        // Registra o evento de duplo clique no ícone da bandeja.
        //
        // Ao realizar um duplo clique, a Dock principal é
        // mostrada novamente.
        //
        // Os parâmetros "_" indicam que tanto o objeto que disparou
        // o evento quanto seus argumentos não são utilizados.
        _trayIcon.DoubleClick += (_, _) => _mainWindow?.ShowDock();
    }

    /// <summary>
    /// Carrega o ícone utilizado na bandeja do Windows.
    ///
    /// O caminho é resolvido a partir de
    /// <see cref="AppContext.BaseDirectory"/> — a pasta real onde
    /// o executável do DockApp está instalado — em vez de um
    /// caminho relativo simples.
    ///
    /// Um caminho relativo simples depende do diretório de trabalho
    /// (CWD) do processo no momento em que ele é iniciado. Quando
    /// o DockApp é aberto manualmente (duplo clique no .exe), o
    /// Windows normalmente define o CWD como a própria pasta do
    /// executável. Porém, quando o DockApp é iniciado automaticamente
    /// pelo Windows (através da entrada configurada em
    /// HKCU\...\Run por <see cref="Services.StartupService"/>), esse
    /// CWD pode ser diferente — fazendo com que o caminho relativo
    /// simplesmente não seja encontrado, e o carregamento do ícone
    /// falhe justamente nesse cenário.
    ///
    /// Se, mesmo assim, o ícone não puder ser carregado por qualquer
    /// outro motivo, um ícone padrão do sistema é utilizado como
    /// último recurso — a falha em carregar o .ico personalizado
    /// nunca deve impedir a bandeja (e, por consequência, o
    /// DockApp inteiro) de continuar funcionando.
    /// </summary>
    /// <returns>
    /// O ícone personalizado do DockApp, ou um ícone padrão
    /// do sistema em caso de falha.
    /// </returns>
    private static System.Drawing.Icon LoadTrayIcon()
    {
        try
        {
            var iconPath =
                Path.Combine(
                    AppContext.BaseDirectory,
                    TrayIconFileName);

            return new System.Drawing.Icon(iconPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[DockApp] Não foi possível carregar o ícone " +
                $"personalizado da bandeja, utilizando um ícone " +
                $"padrão do sistema: {ex.Message}"
            );

            return System.Drawing.SystemIcons.Application;
        }
    }

    /// <summary>
    /// Encerra completamente o DockApp.
    ///
    /// Antes de encerrar a aplicação WPF, o método remove o ícone
    /// da bandeja e libera os recursos utilizados pelo NotifyIcon.
    /// </summary>
    private void ExitApplication()
    {
        // Verifica se o NotifyIcon foi criado.
        if (_trayIcon != null)
        {
            // Remove o ícone da bandeja antes de encerrar.
            //
            // Isso ajuda a evitar que o Windows mantenha
            // temporariamente um ícone residual na área de notificação.
            _trayIcon.Visible = false;

            // Libera os recursos nativos utilizados pelo NotifyIcon.
            _trayIcon.Dispose();
        }

        // Solicita o encerramento completo da aplicação WPF.
        //
        // Como ShutdownMode está configurado como
        // OnExplicitShutdown, esta chamada é responsável
        // por finalizar efetivamente o processo da aplicação.
        Shutdown();
    }

    /// <summary>
    /// Executado quando a aplicação WPF está sendo encerrada.
    ///
    /// Funciona como uma etapa final de limpeza dos recursos
    /// utilizados pelo DockApp.
    /// </summary>
    /// <param name="e">
    /// Argumentos relacionados ao encerramento da aplicação.
    /// </param>
    protected override void OnExit(ExitEventArgs e)
    {
        // Garante que o NotifyIcon seja liberado mesmo que
        // o encerramento aconteça por outro caminho.
        //
        // O operador ?. verifica se _trayIcon possui uma instância
        // antes de executar Dispose().
        _trayIcon?.Dispose();

        // Executa o comportamento padrão de encerramento
        // da classe Application do WPF.
        base.OnExit(e);
    }
}