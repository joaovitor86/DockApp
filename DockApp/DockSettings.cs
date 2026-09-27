namespace DockApp.Models;

/// <summary>
/// Representa todas as configurações persistentes da DockApp.
///
/// A ideia é concentrar aqui as opções de personalização da Dock,
/// evitando espalhar valores fixos pelo MainWindow.xaml/.cs.
/// </summary>
public class DockSettings
{
    // ================================================================
    // ATALHOS
    // ================================================================

    /// <summary>
    /// Pasta onde ficam os atalhos que serão exibidos na Dock.
    /// </summary>
    public string FolderPath { get; set; } = @"D:\MinhaDock";


    // ================================================================
    // FUNDO DA DOCK
    // ================================================================

    /// <summary>
    /// Cor principal do fundo da Dock.
    /// Formato esperado: #RRGGBB.
    /// </summary>
    public string BackgroundColor { get; set; } = "#202020";

    /// <summary>
    /// Segunda cor utilizada quando o degradê estiver habilitado.
    /// </summary>
    public string BackgroundColor2 { get; set; } = "#101010";

    /// <summary>
    /// Define se o fundo utilizará um degradê entre BackgroundColor
    /// e BackgroundColor2.
    /// </summary>
    public bool UseGradient { get; set; } = false;

    /// <summary>
    /// Ângulo do degradê em graus.
    ///
    /// 0   = esquerda para direita
    /// 90  = cima para baixo
    /// 180 = direita para esquerda
    /// 270 = baixo para cima
    /// </summary>
    public double GradientAngle { get; set; } = 90;

    /// <summary>
    /// Opacidade geral do fundo da Dock.
    ///
    /// 0 = totalmente transparente.
    /// 1 = totalmente opaco.
    /// </summary>
    public double BackgroundOpacity { get; set; } = 0.70;


    // ================================================================
    // BORDA DA DOCK
    // ================================================================

    /// <summary>
    /// Cor da borda da Dock.
    /// Formato esperado: #RRGGBB.
    /// </summary>
    public string BorderColor { get; set; } = "#404040";

    /// <summary>
    /// Espessura da borda da Dock em pixels.
    /// </summary>
    public double BorderThickness { get; set; } = 1;

    /// <summary>
    /// Raio dos cantos da Dock.
    ///
    /// 0 = cantos quadrados.
    /// Valores maiores deixam a Dock mais arredondada.
    /// </summary>
    public double CornerRadius { get; set; } = 18;


    // ================================================================
    // ESPAÇAMENTO DA DOCK
    // ================================================================

    /// <summary>
    /// Espaçamento interno horizontal da Dock.
    ///
    /// Em uma Dock horizontal, controla principalmente o espaço
    /// nas extremidades esquerda e direita.
    /// </summary>
    public double DockPaddingHorizontal { get; set; } = 12;

    /// <summary>
    /// Espaçamento interno vertical da Dock.
    ///
    /// Em uma Dock horizontal, controla principalmente o espaço
    /// acima e abaixo dos ícones.
    /// </summary>
    public double DockPaddingVertical { get; set; } = 10;

    /// <summary>
    /// Espaçamento entre os ícones da Dock.
    /// </summary>
    public double IconSpacing { get; set; } = 8;


    // ================================================================
    // ÍCONES
    // ================================================================

    /// <summary>
    /// Tamanho dos ícones em pixels.
    /// </summary>
    public double IconSize { get; set; } = 48;

    /// <summary>
    /// Escala aplicada ao ícone quando o mouse passa sobre ele.
    ///
    /// 1.0 = sem aumento.
    /// 1.4 = aumento de 40%.
    /// </summary>
    public double HoverScale { get; set; } = 1.40;


    // ================================================================
    // SOMBRA
    // ================================================================

    /// <summary>
    /// Define se a Dock terá sombra.
    /// </summary>
    public bool EnableShadow { get; set; } = true;

    /// <summary>
    /// Opacidade da sombra.
    ///
    /// 0 = invisível.
    /// 1 = totalmente opaca.
    /// </summary>
    public double ShadowOpacity { get; set; } = 0.45;

    /// <summary>
    /// Nível de desfoque da sombra.
    /// </summary>
    public double ShadowBlur { get; set; } = 18;

    /// <summary>
    /// Distância da sombra em relação à Dock.
    /// </summary>
    public double ShadowDepth { get; set; } = 4;


    // ================================================================
    // COMPORTAMENTO
    // ================================================================

    /// <summary>
    /// Define em qual lado da tela a Dock ficará.
    /// </summary>
    public DockPosition Position { get; set; } = DockPosition.Bottom;

    /// <summary>
    /// Define se o DockApp será iniciado automaticamente com o Windows.
    /// </summary>
    public bool StartWithWindows { get; set; } = false;
}