using System.Windows.Media;

namespace DockApp.Models;

/// <summary>
/// Representa um item que pode ser exibido na Dock do DockApp.
///
/// Um DockItem contém as informações necessárias para identificar,
/// exibir e executar um programa, jogo, atalho ou URI através da Dock.
/// </summary>
public class DockItem
{
    /// <summary>
    /// Nome de exibição do item na Dock.
    ///
    /// Normalmente corresponde ao nome do programa, jogo ou atalho
    /// que será apresentado ao usuário.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Caminho original do arquivo de atalho utilizado pelo item.
    ///
    /// Pode apontar para arquivos como:
    /// - .lnk
    /// - .url
    ///
    /// Este caminho representa a origem do item, e não necessariamente
    /// o programa que será executado.
    /// </summary>
    public string SourcePath { get; init; } = "";

    /// <summary>
    /// Caminho ou URI que será utilizado para executar o item.
    ///
    /// Pode conter, por exemplo:
    /// - Caminho de um executável (.exe)
    /// - URI do Steam (steam://...)
    /// - URI do Battle.net (battlenet://...)
    /// - Outras URLs ou protocolos registrados no Windows.
    /// </summary>
    public string TargetPath { get; init; } = "";

    /// <summary>
    /// Argumentos adicionais utilizados ao executar o item.
    ///
    /// Pode permanecer vazio quando o programa não necessita
    /// de parâmetros adicionais.
    /// </summary>
    public string Arguments { get; init; } = "";

    /// <summary>
    /// Ícone visual utilizado para representar o item na Dock.
    ///
    /// ImageSource é utilizado pelo WPF para representar imagens
    /// que podem ser exibidas em controles da interface.
    ///
    /// É nullable porque o item pode existir mesmo quando nenhum
    /// ícone foi encontrado ou carregado.
    /// </summary>
    public ImageSource? Icon { get; set; }
}