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
    /// 1. Executar a inicialização padrão do WPF.
    /// 2. Configurar o encerramento explícito da aplicação.
    /// 3. Criar a janela principal.
    /// 4. Exibir a Dock.
    /// 5. Configurar o ícone da bandeja do Windows.
    /// </summary>
    /// <param name="e">
    /// Argumentos fornecidos pelo WPF durante o evento de inicialização.
    /// </param>
    protected override void OnStartup(StartupEventArgs e)
    {
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
            // O caminho é relativo ao diretório de execução
            // da aplicação. Portanto, o arquivo .ico precisa
            // estar disponível nesse diretório durante a execução.
            Icon = new System.Drawing.Icon(
                @"ChatGPT-Image-26-de-set.-de-2026_-20_43_10.ico"
            ),

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