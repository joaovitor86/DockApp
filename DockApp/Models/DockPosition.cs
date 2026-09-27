namespace DockApp.Models;

/// <summary>
/// Define as posições possíveis onde a Dock pode ser exibida
/// na área de trabalho do Windows.
/// </summary>
public enum DockPosition
{
    /// <summary>
    /// Posiciona a Dock na parte inferior da tela.
    /// </summary>
    Bottom,

    /// <summary>
    /// Posiciona a Dock na parte superior da tela.
    /// </summary>
    Top,

    /// <summary>
    /// Posiciona a Dock no lado esquerdo da tela.
    /// </summary>
    Left,

    /// <summary>
    /// Posiciona a Dock no lado direito da tela.
    /// </summary>
    Right
}