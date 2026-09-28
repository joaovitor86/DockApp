// Importa recursos para iniciar processos do Windows,
// como executáveis, URLs e protocolos (steam://, etc.).
using System.Diagnostics;

// Importa recursos para manipulação de diretórios e arquivos.
using System.IO;

// Importa extensões LINQ, utilizadas principalmente para
// localizar transformações dentro de um TransformGroup.
using System.Linq;

// Importa os componentes básicos do WPF.
using System.Windows;

// Importa controles WPF, como StackPanel e ItemsControl.
using System.Windows.Controls;

// Importa recursos relacionados a entrada do usuário,
// incluindo MouseButtonEventArgs e MouseEventArgs.
using System.Windows.Input;

// Importa recursos gráficos do WPF.
using System.Windows.Media;

// Importa recursos de animação do WPF.
using System.Windows.Media.Animation;

// Importa recursos de efeitos visuais do WPF, como DropShadowEffect,
// utilizado para aplicar a sombra configurável da Dock.
using System.Windows.Media.Effects;

// Importa os modelos utilizados pelo DockApp.
using DockApp.Models;

// Importa os serviços utilizados pela janela principal,
// como carregamento de atalhos, configurações e monitoramento da pasta.
using DockApp.Services;

// Existe uma classe Application tanto no WPF quanto no Windows Forms.
// Este alias garante que Application se refere à implementação do WPF.
using Application = System.Windows.Application;

// Define explicitamente Color como o Color do WPF,
// evitando possíveis conflitos com outras classes Color.
using Color = System.Windows.Media.Color;

// Define explicitamente ColorConverter como o conversor de cores do WPF.
using ColorConverter = System.Windows.Media.ColorConverter;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;


// Define explicitamente Orientation como o Orientation dos controles WPF.
using Orientation = System.Windows.Controls.Orientation;
using Point = System.Windows.Point;

namespace DockApp;

/// <summary>
/// Representa a janela principal da DockApp.
///
/// Esta classe é responsável por controlar o comportamento visual
/// e funcional da Dock, incluindo:
///
/// - Carregamento das configurações.
/// - Carregamento dos atalhos.
/// - Monitoramento da pasta de atalhos.
/// - Aplicação das configurações visuais (cor/degradê, borda, sombra,
///   espaçamento, cantos e tamanho dos ícones).
/// - Posicionamento da Dock na tela.
/// - Alteração da orientação dos ícones.
/// - Execução dos atalhos.
/// - Animações dos ícones (hover e lançamento).
/// - Abertura das configurações.
/// - Ocultação e reexibição da Dock.
///
/// A janela utiliza uma configuração sem moldura tradicional,
/// definida no MainWindow.xaml, funcionando como uma Dock flutuante
/// sobre a área de trabalho do Windows.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// DependencyProperty utilizada para armazenar o tamanho
    /// dos ícones da Dock.
    ///
    /// A utilização de DependencyProperty permite que o valor
    /// possa ser utilizado diretamente pelo sistema de binding
    /// do WPF, inclusive pelo MainWindow.xaml.
    /// </summary>
    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(MainWindow),
            new PropertyMetadata(48.0));

    /// <summary>
    /// Tamanho dos ícones exibidos na Dock.
    ///
    /// O valor padrão é 48 pixels independentes de dispositivo (DIP).
    ///
    /// Esta propriedade está vinculada à DependencyProperty
    /// <see cref="IconSizeProperty"/>, permitindo que o XAML
    /// utilize o valor através de binding.
    /// </summary>
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// Distância mínima entre a Dock e a borda da área de trabalho.
    ///
    /// O valor é utilizado durante o cálculo da posição da Dock.
    /// Também evita que a Dock fique completamente colada
    /// à borda da tela ou à área ocupada pela barra de tarefas.
    /// </summary>
    private const double GapFromEdge = 5;

    /// <summary>
    /// Configurações atualmente carregadas pelo DockApp.
    ///
    /// Inicialmente recebe uma instância com os valores padrão.
    /// Durante o carregamento da aplicação, é substituída pelas
    /// configurações persistidas através do ConfigurationService.
    /// </summary>
    private DockSettings _settings = new();

    /// <summary>
    /// Serviço responsável por monitorar a pasta configurada
    /// para detectar criação, exclusão, alteração ou renomeação
    /// de atalhos.
    ///
    /// É nullable porque o serviço só é criado quando uma pasta
    /// válida é configurada.
    /// </summary>
    private FolderWatcherService? _folderWatcher;

    /// <summary>
    /// Inicializa uma nova instância da janela principal.
    ///
    /// Após a inicialização do XAML:
    /// - Registra o evento Loaded para carregar a Dock.
    /// - Registra o evento Closed para liberar o FolderWatcherService.
    /// </summary>
    public MainWindow()
    {
        // Inicializa os componentes definidos no MainWindow.xaml.
        InitializeComponent();

        // Quando a janela estiver completamente carregada,
        // executa o carregamento inicial da Dock.
        Loaded += (_, _) => Reload();

        // Quando a janela for fechada, libera os recursos
        // utilizados pelo monitoramento da pasta.
        Closed += (_, _) => _folderWatcher?.Dispose();
    }

    /// <summary>
    /// Recarrega completamente o estado da Dock.
    ///
    /// O processo consiste em:
    /// 1. Carregar as configurações salvas.
    /// 2. Aplicar a aparência (cor/degradê, borda, sombra, padding).
    /// 3. Aplicar a orientação e o espaçamento dos ícones.
    /// 4. Carregar os atalhos.
    /// 5. Configurar o monitoramento da pasta.
    /// 6. Reposicionar a janela depois que o layout estiver pronto.
    /// </summary>
    private void Reload()
    {
        // Carrega as configurações persistidas pelo aplicativo.
        _settings = ConfigurationService.Load();

        // Aplica cor/degradê, opacidade, borda, sombra, padding,
        // raio das bordas e tamanho dos ícones.
        ApplyAppearance();

        // Ajusta a orientação e o espaçamento dos itens conforme
        // a posição da Dock e o IconSpacing configurado.
        ApplyLayout();

        // Carrega os atalhos existentes na pasta configurada.
        LoadIcons();

        // Configura ou atualiza o monitoramento da pasta.
        SetupFolderWatcher();

        // Agenda o posicionamento para depois que o WPF terminar
        // de calcular o tamanho e o layout da janela.
        Dispatcher.BeginInvoke(
            () =>
            {
                PositionWindow();
            },
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Carrega os atalhos da pasta configurada e os atribui
    /// ao ItemsControl responsável por exibir os ícones.
    /// </summary>
    private void LoadIcons()
    {
        // O ShortcutLoader interpreta os arquivos .lnk e .url
        // existentes na pasta configurada.
        //
        // A lista resultante é atribuída ao ItemsSource do ItemsControl,
        // fazendo com que o WPF gere visualmente os ícones.
        IconsPanel.ItemsSource =
            ShortcutLoader.LoadFolder(_settings.FolderPath);
    }

    /// <summary>
    /// Ajusta a orientação e o espaçamento dos ícones da Dock.
    ///
    /// Dock na esquerda ou direita:
    /// - Ícones ficam organizados verticalmente.
    ///
    /// Dock em cima ou embaixo:
    /// - Ícones ficam organizados horizontalmente.
    ///
    /// Além da orientação, este método também aplica o espaçamento
    /// configurado em <see cref="DockSettings.IconSpacing"/> através
    /// de um ItemContainerStyle, evitando que o valor fique fixo
    /// diretamente no DataTemplate do XAML.
    /// </summary>
    private void ApplyLayout()
    {
        // Determina a orientação com base na posição configurada.
        var orientation =
            _settings.Position is DockPosition.Left or DockPosition.Right
                ? Orientation.Vertical
                : Orientation.Horizontal;

        // Cria dinamicamente um StackPanel para ser utilizado
        // como painel de organização dos itens.
        var factory =
            new FrameworkElementFactory(typeof(StackPanel));

        // Define a orientação do StackPanel.
        factory.SetValue(
            StackPanel.OrientationProperty,
            orientation);

        // Cria o ItemsPanelTemplate utilizando o StackPanel criado.
        //
        // Isso permite alterar dinamicamente a orientação dos ícones
        // sem precisar manter dois templates diferentes no XAML.
        IconsPanel.ItemsPanel =
            new ItemsPanelTemplate(factory);

        // Cria um estilo aplicado ao container gerado para cada item.
        //
        // Como o ItemsControl não possui seleção (ao contrário de
        // ListBox/ComboBox), o container gerado para cada item de
        // ItemsSource é um ContentPresenter.
        //
        // O espaçamento configurado em IconSpacing é dividido pela
        // metade e aplicado como margem uniforme: dessa forma, dois
        // ícones vizinhos ficam separados exatamente pela distância
        // configurada, sem depender de um valor fixo no DataTemplate.
        var itemContainerStyle =
            new Style(typeof(ContentPresenter));

        itemContainerStyle.Setters.Add(
            new Setter(
                FrameworkElement.MarginProperty,
                new Thickness(_settings.IconSpacing / 2)));

        IconsPanel.ItemContainerStyle = itemContainerStyle;

        // Garante que a janela seja reposicionada depois que
        // a alteração da orientação/espaçamento modificar o tamanho
        // do conteúdo.
        Dispatcher.BeginInvoke(PositionWindow);
    }

    /// <summary>
    /// Inicializa ou atualiza o monitoramento da pasta de atalhos.
    ///
    /// Caso a pasta não exista, o monitoramento é interrompido.
    ///
    /// Caso o serviço ainda não exista, uma nova instância é criada.
    ///
    /// Caso o serviço já exista, apenas sua pasta monitorada
    /// é atualizada.
    /// </summary>
    private void SetupFolderWatcher()
    {
        // Verifica se a pasta configurada realmente existe.
        if (!Directory.Exists(_settings.FolderPath))
        {
            // Se a pasta não existir, interrompe o monitoramento atual.
            _folderWatcher?.Stop();

            return;
        }

        // Se ainda não existe um monitoramento, cria o serviço.
        if (_folderWatcher == null)
        {
            // Cria o FolderWatcherService utilizando o Dispatcher
            // da interface WPF.
            _folderWatcher =
                new FolderWatcherService(
                    _settings.FolderPath,
                    Dispatcher);

            // Quando houver alteração na pasta, recarrega os atalhos.
            _folderWatcher.OnChanged += LoadIcons;

            // Inicia o monitoramento.
            _folderWatcher.Start();
        }
        else
        {
            // Se o serviço já existe, apenas muda a pasta monitorada.
            _folderWatcher.ChangeFolder(_settings.FolderPath);
        }
    }

    /// <summary>
    /// Aplica as configurações visuais da Dock.
    ///
    /// São aplicados:
    /// - Cor de fundo (sólida ou em degradê, conforme UseGradient).
    /// - Opacidade.
    /// - Cor e espessura da borda.
    /// - Raio dos cantos.
    /// - Sombra (quando habilitada).
    /// - Espaçamento interno (padding horizontal/vertical).
    /// - Tamanho dos ícones.
    ///
    /// Caso alguma cor configurada seja inválida, é utilizada
    /// uma cor padrão escura com transparência.
    /// </summary>
    private void ApplyAppearance()
    {
        // ============================================================
        // FUNDO (sólido ou degradê)
        // ============================================================
        try
        {
            // Converte a opacidade configurada (0-1) para o canal
            // Alpha utilizado pelas cores (0-255).
            var alpha =
                (byte)Math.Clamp(
                    _settings.BackgroundOpacity * 255,
                    0,
                    255);

            if (_settings.UseGradient)
            {
                // Converte as duas cores configuradas para o degradê.
                var color1 =
                    (Color)ColorConverter.ConvertFromString(
                        _settings.BackgroundColor);
                color1.A = alpha;

                var color2 =
                    (Color)ColorConverter.ConvertFromString(
                        _settings.BackgroundColor2);
                color2.A = alpha;

                // Cria um degradê horizontal (esquerda -> direita)
                // e o rotaciona em torno do centro do elemento de
                // acordo com o ângulo configurado.
                //
                // Isso faz com que 0° represente esquerda->direita,
                // 90° represente cima->baixo, 180° direita->esquerda
                // e 270° baixo->cima, exatamente como documentado
                // em DockSettings.GradientAngle.
                var gradient =
                    new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0.5),
                        EndPoint = new Point(1, 0.5),
                        RelativeTransform =
                            new RotateTransform(
                                _settings.GradientAngle,
                                0.5,
                                0.5)
                    };

                gradient.GradientStops.Add(new GradientStop(color1, 0));
                gradient.GradientStops.Add(new GradientStop(color2, 1));

                DockBorder.Background = gradient;
            }
            else
            {
                // Converte a string hexadecimal configurada
                // em uma estrutura Color do WPF.
                var color =
                    (Color)ColorConverter.ConvertFromString(
                        _settings.BackgroundColor);

                color.A = alpha;

                // Cria o pincel utilizado como fundo da Dock.
                DockBorder.Background =
                    new SolidColorBrush(color);
            }
        }
        catch
        {
            // Caso alguma cor configurada seja inválida,
            // utiliza uma cor padrão:
            //
            // Alpha = 0xB0
            // Red   = 0x20
            // Green = 0x20
            // Blue  = 0x20
            DockBorder.Background =
                new SolidColorBrush(
                    Color.FromArgb(
                        0xB0,
                        0x20,
                        0x20,
                        0x20));
        }

        // ============================================================
        // BORDA
        // ============================================================
        try
        {
            // Converte a cor hexadecimal configurada para a borda.
            var borderColor =
                (Color)ColorConverter.ConvertFromString(
                    _settings.BorderColor);

            DockBorder.BorderBrush =
                new SolidColorBrush(borderColor);
        }
        catch
        {
            // Caso a cor da borda seja inválida, utiliza um
            // cinza escuro neutro como padrão.
            DockBorder.BorderBrush =
                new SolidColorBrush(
                    Color.FromRgb(0x40, 0x40, 0x40));
        }

        // Aplica a espessura da borda configurada.
        DockBorder.BorderThickness =
            new Thickness(_settings.BorderThickness);

        // ============================================================
        // CANTOS E TAMANHO DOS ÍCONES
        // ============================================================

        // Aplica o raio das bordas configurado pelo usuário.
        DockBorder.CornerRadius =
            new CornerRadius(_settings.CornerRadius);

        // Atualiza o tamanho dos ícones.
        IconSize = _settings.IconSize;

        // ============================================================
        // SOMBRA
        // ============================================================

        // Quando habilitada, aplica uma sombra projetada para baixo,
        // dando à Dock um efeito de profundidade sobre a área
        // de trabalho. Quando desabilitada, remove qualquer efeito
        // aplicado anteriormente (definindo Effect como null).
        DockBorder.Effect =
            _settings.EnableShadow
                ? new DropShadowEffect
                {
                    Color = Colors.Black,
                    Opacity = _settings.ShadowOpacity,
                    BlurRadius = _settings.ShadowBlur,
                    ShadowDepth = _settings.ShadowDepth,
                    Direction = 270
                }
                : null;

        // ============================================================
        // ESPAÇAMENTO INTERNO (PADDING)
        // ============================================================

        // Aplica o espaçamento interno horizontal e vertical
        // configurado pelo usuário.
        DockBorder.Padding =
            new Thickness(
                _settings.DockPaddingHorizontal,
                _settings.DockPaddingVertical,
                _settings.DockPaddingHorizontal,
                _settings.DockPaddingVertical);
    }

    /// <summary>
    /// Evento disparado quando o tamanho da janela é alterado.
    ///
    /// Como a Dock utiliza SizeToContent, alterações no conteúdo
    /// podem modificar o tamanho da janela. Quando isso acontece,
    /// sua posição precisa ser recalculada.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Informações sobre a alteração de tamanho.
    /// </param>
    private void Window_SizeChanged(
        object sender,
        SizeChangedEventArgs e)
    {
        // Recalcula a posição da Dock.
        PositionWindow();
    }

    /// <summary>
    /// Calcula e aplica a posição da Dock na área de trabalho.
    ///
    /// A posição depende do valor configurado em
    /// <see cref="DockSettings.Position"/>.
    ///
    /// A Dock pode ser posicionada:
    /// - Embaixo.
    /// - Em cima.
    /// - À esquerda.
    /// - À direita.
    ///
    /// O cálculo utiliza SystemParameters.WorkArea para considerar
    /// apenas a área disponível da tela, excluindo a região ocupada
    /// pela barra de tarefas do Windows.
    /// </summary>
    private void PositionWindow()
    {
        // Evita tentar posicionar uma janela que ainda não
        // terminou de ser carregada pelo WPF.
        if (!IsLoaded)
            return;

        // Verifica se o WPF ainda não calculou as dimensões reais.
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            // Agenda uma nova tentativa depois que o layout
            // for concluído.
            Dispatcher.BeginInvoke(
                PositionWindow,
                System.Windows.Threading.DispatcherPriority.Loaded);

            return;
        }

        // Obtém a área de trabalho disponível.
        //
        // WorkArea normalmente representa a área da tela
        // descontando a barra de tarefas.
        var workArea = SystemParameters.WorkArea;

        // Calcula a posição de acordo com o lado escolhido.
        switch (_settings.Position)
        {
            case DockPosition.Bottom:

                // Centraliza horizontalmente a Dock.
                Left =
                    workArea.Left +
                    (workArea.Width - ActualWidth) / 2;

                // Encosta a Dock na parte inferior da área útil,
                // mantendo o espaço definido por GapFromEdge.
                Top =
                    workArea.Bottom -
                    ActualHeight -
                    GapFromEdge;

                break;

            case DockPosition.Top:

                // Centraliza horizontalmente a Dock.
                Left =
                    workArea.Left +
                    (workArea.Width - ActualWidth) / 2;

                // Posiciona a Dock próxima à parte superior.
                Top =
                    workArea.Top +
                    GapFromEdge;

                break;

            case DockPosition.Left:

                // Posiciona a Dock próxima à borda esquerda.
                Left =
                    workArea.Left +
                    GapFromEdge;

                // Centraliza verticalmente.
                Top =
                    workArea.Top +
                    (workArea.Height - ActualHeight) / 2;

                break;

            case DockPosition.Right:

                // Posiciona a Dock próxima à borda direita.
                Left =
                    workArea.Right -
                    ActualWidth -
                    GapFromEdge;

                // Centraliza verticalmente.
                Top =
                    workArea.Top +
                    (workArea.Height - ActualHeight) / 2;

                break;
        }
    }

    /// <summary>
    /// Executado quando o usuário clica em um ícone da Dock.
    ///
    /// Obtém o DockItem associado ao elemento clicado e tenta
    /// executar seu TargetPath utilizando o Shell do Windows.
    ///
    /// Após a execução bem-sucedida, reproduz a animação
    /// de "quique" do ícone.
    /// </summary>
    /// <param name="sender">
    /// Elemento visual que recebeu o clique.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento de mouse.
    /// </param>
    private void Icon_Click(
        object sender,
        MouseButtonEventArgs e)
    {
        // Tenta obter o DockItem armazenado na propriedade Tag
        // do elemento clicado.
        //
        // Caso o elemento não possua um DockItem válido,
        // simplesmente encerra o método.
        if (sender is not FrameworkElement
            {
                Tag: DockItem item
            } element)
        {
            return;
        }

        try
        {
            // Solicita ao Windows que abra o TargetPath.
            //
            // UseShellExecute = true permite utilizar:
            // - Executáveis.
            // - URLs.
            // - Protocolos personalizados.
            // - Outros destinos reconhecidos pelo Shell.
            Process.Start(
                new ProcessStartInfo(item.TargetPath)
                {
                    Arguments = item.Arguments,
                    UseShellExecute = true
                });

            // Executa o efeito visual de lançamento do ícone.
            PlayLaunchBounce(element);
        }
        catch
        {
            // Falha ao abrir o destino.
            //
            // TODO:
            // Implementar posteriormente um feedback visual
            // informando ao usuário que o aplicativo/atalho
            // não pôde ser aberto.
        }
    }

    /// <summary>
    /// Garante que o TransformGroup utilizado pelo RenderTransform
    /// de um ícone possa ser animado em código (via BeginAnimation).
    ///
    /// O WPF pode congelar ("Freeze") automaticamente objetos
    /// Freezable declarados em XAML com valores puramente literais
    /// (sem binding, sem nome), como otimização de performance —
    /// isso acontece mesmo quando o TransformGroup está dentro de
    /// um DataTemplate, não apenas em Style ou ResourceDictionary.
    ///
    /// Um objeto congelado não pode ser alvo de BeginAnimation()
    /// chamado diretamente em código (gera InvalidOperationException).
    ///
    /// Este método detecta esse cenário e, quando necessário, clona
    /// o TransformGroup (criando uma cópia independente e editável)
    /// e o reatribui como RenderTransform apenas daquele elemento
    /// específico. A partir daí, aquele ícone passa a ter sua
    /// própria instância, não compartilhada e não congelada.
    /// </summary>
    /// <param name="element">
    /// Elemento visual (ícone) cujo RenderTransform será garantido
    /// como editável.
    /// </param>
    /// <returns>
    /// O TransformGroup pronto para ser animado, ou <c>null</c>
    /// caso o elemento não possua um TransformGroup como
    /// RenderTransform.
    /// </returns>
    private static TransformGroup? EnsureAnimatableTransformGroup(
        FrameworkElement? element)
    {
        if (element?.RenderTransform is not TransformGroup group)
            return null;

        // Se o grupo já estiver congelado, cria uma cópia editável
        // e a atribui exclusivamente a este elemento.
        //
        // Chamadas futuras para este mesmo elemento não entrarão
        // mais aqui, pois o clone já nasce "não congelado".
        if (group.IsFrozen)
        {
            var clone = group.Clone();
            element.RenderTransform = clone;
            return clone;
        }

        return group;
    }

    /// <summary>
    /// Executa a animação de "quique" no ícone recém-aberto.
    ///
    /// O movimento é direcionado para longe da borda da tela
    /// em que a Dock está posicionada.
    ///
    /// Exemplos:
    /// - Dock embaixo: ícone pula para cima.
    /// - Dock em cima: ícone pula para baixo.
    /// - Dock à esquerda: ícone pula para a direita.
    /// - Dock à direita: ícone pula para a esquerda.
    /// </summary>
    /// <param name="element">
    /// Elemento visual que representa o ícone clicado.
    /// </param>
    private void PlayLaunchBounce(FrameworkElement element)
    {
        // Garante que o TransformGroup deste ícone possa ser
        // animado (ver EnsureAnimatableTransformGroup para detalhes
        // sobre o congelamento automático do WPF).
        var group = EnsureAnimatableTransformGroup(element);

        if (group == null)
            return;

        // Procura dentro do TransformGroup a transformação
        // responsável pelo deslocamento do elemento.
        var translate =
            group.Children
                .OfType<TranslateTransform>()
                .FirstOrDefault();

        // Se não existir uma TranslateTransform,
        // não há transformação de posição para animar.
        if (translate == null)
            return;

        // Verifica se a Dock está posicionada verticalmente.
        //
        // Nesse caso, o deslocamento visual relevante ocorre
        // no eixo X.
        var isVertical =
            _settings.Position is DockPosition.Left or DockPosition.Right;

        // Escolhe qual propriedade de transformação será animada.
        //
        // Dock vertical:
        // TranslateTransform.X
        //
        // Dock horizontal:
        // TranslateTransform.Y
        var property =
            isVertical
                ? TranslateTransform.XProperty
                : TranslateTransform.YProperty;

        // Define a direção do quique de acordo com a posição da Dock.
        //
        // O objetivo é fazer o ícone se afastar da borda da tela.
        double toValue =
            _settings.Position switch
            {
                // Dock no topo -> movimento para baixo.
                DockPosition.Top => 16,

                // Dock à esquerda -> movimento para a direita.
                DockPosition.Left => 16,

                // Dock à direita -> movimento para a esquerda.
                DockPosition.Right => -16,

                // Dock embaixo -> movimento para cima.
                _ => -16
            };

        // Cria a animação de deslocamento.
        var bounce =
            new DoubleAnimation
            {
                // Sempre começa na posição original.
                From = 0,

                // Desloca o ícone na direção definida acima.
                To = toValue,

                // Cada movimento dura 200 ms.
                Duration =
                    TimeSpan.FromMilliseconds(200),

                // Depois de chegar ao destino,
                // retorna automaticamente à posição original.
                AutoReverse = true,

                // Repete o efeito cinco vezes.
                RepeatBehavior =
                    new RepeatBehavior(5),

                // Ao terminar, não mantém o valor final
                // aplicado pela animação.
                FillBehavior = FillBehavior.Stop,

                // Faz o movimento começar mais suavemente.
                EasingFunction =
                    new QuadraticEase
                    {
                        EasingMode = EasingMode.EaseOut
                    }
            };

        // Inicia a animação diretamente na propriedade
        // de deslocamento encontrada anteriormente.
        translate.BeginAnimation(property, bounce);
    }

    /// <summary>
    /// Executado quando o ponteiro do mouse entra sobre um ícone
    /// da Dock.
    ///
    /// Amplia o ícone suavemente até a escala configurada em
    /// <see cref="DockSettings.HoverScale"/>.
    /// </summary>
    /// <param name="sender">
    /// Elemento visual (Image) sobre o qual o mouse entrou.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento de mouse.
    /// </param>
    private void Icon_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimateHoverScale(
            sender as FrameworkElement,
            _settings.HoverScale);
    }

    /// <summary>
    /// Executado quando o ponteiro do mouse sai de um ícone
    /// da Dock.
    ///
    /// Retorna o ícone suavemente à escala original (1.0).
    /// </summary>
    /// <param name="sender">
    /// Elemento visual (Image) do qual o mouse saiu.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento de mouse.
    /// </param>
    private void Icon_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimateHoverScale(
            sender as FrameworkElement,
            1.0);
    }

    /// <summary>
    /// Anima a escala (ScaleX/ScaleY) de um ícone da Dock até
    /// o valor informado.
    ///
    /// A escala alvo é lida em tempo real a partir de
    /// <see cref="DockSettings.HoverScale"/> (em vez de um valor
    /// fixo no XAML), permitindo que o usuário configure a
    /// intensidade do efeito de hover pela janela de configurações.
    /// </summary>
    /// <param name="element">
    /// Elemento visual que representa o ícone.
    /// </param>
    /// <param name="scale">
    /// Escala alvo da animação (1.0 = tamanho normal).
    /// </param>
    private static void AnimateHoverScale(
        FrameworkElement? element,
        double scale)
    {
        // Garante que o TransformGroup deste ícone possa ser
        // animado (ver EnsureAnimatableTransformGroup para detalhes
        // sobre o congelamento automático do WPF).
        var group = EnsureAnimatableTransformGroup(element);

        if (group == null)
            return;

        // Procura dentro do TransformGroup a transformação
        // responsável pela escala do elemento.
        var scaleTransform =
            group.Children
                .OfType<ScaleTransform>()
                .FirstOrDefault();

        // Se não existir uma ScaleTransform,
        // não há transformação de escala para animar.
        if (scaleTransform == null)
            return;

        // Cria a animação de escala.
        var animation =
            new DoubleAnimation
            {
                To = scale,

                Duration =
                    TimeSpan.FromMilliseconds(120),

                EasingFunction =
                    new QuadraticEase
                    {
                        EasingMode = EasingMode.EaseOut
                    }
            };

        // Anima os dois eixos simultaneamente para manter
        // o ícone proporcional durante o efeito.
        scaleTransform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            animation);

        scaleTransform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            animation);
    }

    /// <summary>
    /// Controla a abertura do menu de contexto da Dock.
    ///
    /// Quando o menu é solicitado diretamente sobre um ícone,
    /// o evento é cancelado para evitar que o menu geral da Dock
    /// seja exibido sobre o item.
    /// </summary>
    /// <param name="sender">
    /// Elemento que recebeu a solicitação do menu de contexto.
    /// </param>
    /// <param name="e">
    /// Argumentos relacionados à abertura do menu.
    /// </param>
    private void Border_ContextMenuOpening(
        object sender,
        ContextMenuEventArgs e)
    {
        // Se a origem do evento pertence a um DockItem,
        // cancela a abertura do menu.
        if (e.OriginalSource is FrameworkElement
            {
                DataContext: DockItem
            })
        {
            e.Handled = true;
        }
    }

    /// <summary>
    /// Abre a janela de configurações do DockApp.
    ///
    /// Após o fechamento da janela:
    /// - Se o usuário salvou: recarrega todas as configurações.
    /// - Se cancelou: restaura a aparência anteriormente salva.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento.
    /// </param>
    private void OpenSettings_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Cria a janela de configurações.
        //
        // PreviewAppearance é passado como callback para permitir
        // que a janela de configurações altere temporariamente
        // a aparência da Dock durante a pré-visualização.
        var settingsWindow =
            new SettingsWindow(PreviewAppearance);

        // Exibe a janela como diálogo modal.
        //
        // ShowDialog() retorna true quando as configurações
        // são salvas e false/null quando a janela é cancelada.
        var saved =
            settingsWindow.ShowDialog() == true;

        if (saved)
        {
            // Recarrega todas as configurações salvas,
            // incluindo aparência, posição e atalhos.
            Reload();
        }
        else
        {
            // Caso o usuário tenha cancelado,
            // restaura a aparência baseada nas configurações salvas.
            //
            // Isso desfaz qualquer alteração temporária
            // feita pela pré-visualização.
            ApplyAppearance();
        }
    }

    /// <summary>
    /// Aplica temporariamente uma aparência à Dock.
    ///
    /// Este método é utilizado pela janela de configurações
    /// para permitir que o usuário visualize alterações
    /// de cor de fundo e opacidade antes de salvá-las definitivamente.
    ///
    /// Observação: a pré-visualização ao vivo cobre apenas cor
    /// de fundo e opacidade (como já era antes). Borda, sombra,
    /// degradê e espaçamento são aplicados somente após salvar,
    /// através de Reload().
    ///
    /// As alterações aplicadas aqui não são persistidas.
    /// </summary>
    /// <param name="colorHex">
    /// Cor em formato hexadecimal.
    /// </param>
    /// <param name="opacity">
    /// Opacidade desejada, normalmente entre 0 e 1.
    /// </param>
    private void PreviewAppearance(
        string colorHex,
        double opacity)
    {
        try
        {
            // Converte a cor hexadecimal para Color.
            var color =
                (Color)ColorConverter.ConvertFromString(
                    colorHex);

            // Converte a opacidade de 0-1 para Alpha 0-255.
            color.A =
                (byte)Math.Clamp(
                    opacity * 255,
                    0,
                    255);

            // Aplica temporariamente a nova cor.
            DockBorder.Background =
                new SolidColorBrush(color);
        }
        catch
        {
            // Se o usuário estiver digitando uma cor inválida,
            // simplesmente ignora a alteração temporariamente.
            //
            // Quando o valor se tornar válido, a pré-visualização
            // poderá ser aplicada novamente.
        }
    }

    /// <summary>
    /// Oculta a Dock sem encerrar o DockApp.
    ///
    /// O processo continua executando em segundo plano e o ícone
    /// da bandeja permanece disponível para reexibir a Dock.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento.
    /// </param>
    private void HideMenuItem_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Esconde somente a janela da Dock.
        //
        // Como o ShutdownMode está configurado como
        // OnExplicitShutdown no App.xaml.cs, isso não encerra
        // a aplicação.
        Hide();
    }

    /// <summary>
    /// Reexibe a Dock após ela ter sido ocultada.
    ///
    /// Também agenda o reposicionamento da janela para garantir
    /// que o WPF já tenha recalculado seu tamanho e layout.
    /// </summary>
    public void ShowDock()
    {
        // Torna a janela novamente visível.
        Show();

        // Aguarda o WPF concluir o processamento do layout
        // antes de recalcular sua posição.
        Dispatcher.BeginInvoke(
            () =>
            {
                // Reposiciona a Dock.
                PositionWindow();

                // Coloca a janela novamente em primeiro plano.
                Activate();
            },
            System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Abre a janela de configurações através do menu da bandeja.
    ///
    /// Antes de abrir as configurações, garante que a Dock esteja
    /// visível novamente.
    /// </summary>
    public void OpenSettingsFromTray()
    {
        // Garante que a janela esteja visível e corretamente posicionada.
        ShowDock();

        // Reutiliza o mesmo método utilizado pelo menu de contexto
        // da própria Dock para abrir as configurações.
        OpenSettings_Click(
            this,
            new RoutedEventArgs());
    }
}