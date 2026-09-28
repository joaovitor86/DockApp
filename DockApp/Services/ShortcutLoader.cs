using DockApp.Models;
using IWshRuntimeLibrary;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DockApp.Services;

/// <summary>
/// Responsável por localizar e carregar atalhos existentes em uma pasta.
///
/// O serviço suporta atualmente:
/// - Arquivos .lnk (atalhos do Windows);
/// - Arquivos .url (atalhos de Internet).
///
/// Para cada atalho encontrado, um <see cref="DockItem"/> é criado
/// contendo nome, caminho de origem, destino, argumentos e ícone.
///
/// A extração dos ícones é realizada utilizando APIs nativas do Windows.
///
/// Erros encontrados em um atalho individual são tratados internamente,
/// permitindo que os demais atalhos continuem sendo carregados normalmente.
/// </summary>
public static class ShortcutLoader
{
    /// <summary>
    /// Carrega todos os atalhos suportados encontrados diretamente
    /// dentro da pasta especificada.
    ///
    /// Arquivos que não sejam .lnk ou .url são ignorados.
    /// Falhas na leitura de um arquivo específico não interrompem
    /// o carregamento dos demais arquivos.
    /// </summary>
    /// <param name="folder">
    /// Caminho da pasta que contém os atalhos.
    /// </param>
    /// <returns>
    /// Lista de <see cref="DockItem"/> carregados com sucesso.
    /// Caso a pasta não exista ou nenhum item válido seja encontrado,
    /// uma lista vazia será retornada.
    /// </returns>
    public static List<DockItem> LoadFolder(string folder)
    {
        // Lista que armazenará os atalhos carregados com sucesso.
        var items = new List<DockItem>();

        // Verifica se a pasta informada realmente existe.
        if (!Directory.Exists(folder))
        {
            // Registra a informação no Debug para facilitar
            // a identificação de problemas durante o desenvolvimento.
            Debug.WriteLine($"[DockApp] Pasta não encontrada: {folder}");

            // Retorna uma lista vazia em vez de lançar uma exceção.
            return items;
        }

        // Obtém todos os arquivos existentes diretamente dentro da pasta.
        //
        // EnumerateFiles() não inclui subpastas porque nenhum padrão
        // de busca recursiva foi especificado.
        var files = Directory.EnumerateFiles(folder).ToList();

        // Registra no Debug a quantidade total de arquivos encontrados.
        Debug.WriteLine(
            $"[DockApp] {files.Count} arquivo(s) encontrados na pasta."
        );

        // Cria uma única instância do Shell do Windows (COM) para
        // ser reutilizada por todos os arquivos .lnk desta varredura.
        //
        // Criar um WshShell é uma operação de interoperabilidade COM
        // com um custo perceptível quando repetida muitas vezes.
        // Antes, uma nova instância era criada a cada arquivo .lnk
        // processado; agora apenas uma instância é criada por chamada
        // a LoadFolder(), independentemente da quantidade de atalhos.
        var shell = new WshShell();

        // Processa cada arquivo individualmente.
        foreach (var file in files)
        {
            // Obtém a extensão do arquivo e converte para minúsculas
            // de forma independente da cultura do sistema.
            //
            // Isso garante que .LNK, .Lnk e .lnk sejam tratados
            // da mesma maneira.
            var ext = Path.GetExtension(file).ToLowerInvariant();

            // Seleciona o método responsável por interpretar
            // o arquivo de acordo com sua extensão.
            //
            // .lnk → ReadLnk() (reaproveitando o WshShell criado acima)
            // .url → ReadUrl()
            // qualquer outra extensão → null
            var item = ext switch
            {
                ".lnk" => ReadLnk(file, shell),
                ".url" => ReadUrl(file),
                _ => null
            };

            // Verifica se o arquivo foi convertido com sucesso
            // em um DockItem.
            if (item != null)
            {
                // Se o item foi carregado, mas não possui ícone,
                // registra essa informação para diagnóstico.
                if (item.Icon == null)
                {
                    Debug.WriteLine(
                        $"[DockApp] Sem ícone extraído para: {file}"
                    );
                }

                // Adiciona o item à lista final.
                items.Add(item);
            }
            else if (ext is ".lnk" or ".url")
            {
                // O arquivo possui uma extensão suportada,
                // mas não pôde ser interpretado corretamente.
                //
                // O erro não interrompe o carregamento dos outros itens.
                Debug.WriteLine(
                    $"[DockApp] IGNORADO (falhou ao ler): {file}"
                );
            }
        }

        // Registra a quantidade final de atalhos carregados.
        Debug.WriteLine(
            $"[DockApp] {items.Count} item(ns) carregados com sucesso."
        );

        return items;
    }

    /// <summary>
    /// Lê um arquivo de atalho do Windows (.lnk) e o converte
    /// em um <see cref="DockItem"/>.
    ///
    /// O Windows Shell é utilizado através da biblioteca
    /// IWshRuntimeLibrary para obter:
    /// - Caminho do executável/arquivo de destino;
    /// - Argumentos;
    /// - Localização do ícone;
    /// - Índice do ícone.
    /// </summary>
    /// <param name="path">
    /// Caminho completo do arquivo .lnk.
    /// </param>
    /// <param name="shell">
    /// Instância do Shell do Windows (COM) reutilizada entre todos
    /// os arquivos .lnk processados por <see cref="LoadFolder"/>,
    /// em vez de criar uma nova instância a cada chamada.
    /// </param>
    /// <returns>
    /// Um <see cref="DockItem"/> quando o atalho puder ser lido;
    /// caso contrário, <c>null</c>.
    /// </returns>
    private static DockItem? ReadLnk(string path, WshShell shell)
    {
        try
        {
            // Abre o arquivo .lnk e o interpreta como IWshShortcut.
            var link = (IWshShortcut)shell.CreateShortcut(path);

            // Obtém o caminho do arquivo que contém o ícone
            // e o índice do ícone dentro desse arquivo.
            //
            // Caso IconLocation não seja válido, TargetPath
            // será utilizado como alternativa.
            var (iconPath, iconIndex) = ParseIconLocation(
                link.IconLocation,
                link.TargetPath
            );

            // Cria o objeto que representa o item da Dock.
            return new DockItem
            {
                // Utiliza o nome do arquivo .lnk sem sua extensão.
                Name = Path.GetFileNameWithoutExtension(path),

                // Guarda o caminho original do atalho.
                SourcePath = path,

                // Guarda o destino real definido no atalho.
                TargetPath = link.TargetPath,

                // Guarda os argumentos configurados no atalho.
                Arguments = link.Arguments,

                // Primeiro tenta extrair o ícone indicado
                // pela propriedade IconLocation.
                //
                // Se essa tentativa falhar, tenta extrair
                // o primeiro ícone do próprio TargetPath.
                Icon = ExtractIcon(iconPath, iconIndex)
                       ?? ExtractIcon(link.TargetPath, 0)
            };
        }
        catch (Exception ex)
        {
            // Qualquer erro durante a leitura desse atalho
            // é tratado individualmente.
            //
            // Dessa maneira, um .lnk problemático não impede
            // os outros atalhos de serem carregados.
            Debug.WriteLine(
                $"[DockApp] Erro ao ler .lnk '{path}': {ex.Message}"
            );

            return null;
        }
    }

    /// <summary>
    /// Lê um arquivo de atalho de Internet (.url).
    ///
    /// Arquivos .url são arquivos de texto no formato INI simplificado
    /// que podem conter informações como:
    /// - URL de destino;
    /// - Arquivo do ícone;
    /// - Índice do ícone.
    /// </summary>
    /// <param name="path">
    /// Caminho completo do arquivo .url.
    /// </param>
    /// <returns>
    /// Um <see cref="DockItem"/> quando o arquivo possuir uma URL válida;
    /// caso contrário, <c>null</c>.
    /// </returns>
    private static DockItem? ReadUrl(string path)
    {
        try
        {
            // Variáveis utilizadas para armazenar os valores
            // encontrados dentro do arquivo .url.
            string? url = null;
            string? iconFile = null;
            string? iconIndexStr = null;

            // Lê todas as linhas do arquivo .url.
            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                // Procura a chave URL=.
                if (line.StartsWith(
                    "URL=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    // Remove "URL=" e espaços ao redor do valor.
                    url = line[4..].Trim();
                }

                // Procura a chave IconFile=.
                else if (line.StartsWith(
                    "IconFile=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    // Remove "IconFile=" e espaços ao redor do valor.
                    iconFile = line[9..].Trim();
                }

                // Procura a chave IconIndex=.
                else if (line.StartsWith(
                    "IconIndex=",
                    StringComparison.OrdinalIgnoreCase))
                {
                    // Remove "IconIndex=" e espaços ao redor do valor.
                    iconIndexStr = line[10..].Trim();
                }
            }

            // Uma URL é obrigatória para que o .url seja considerado válido.
            if (string.IsNullOrEmpty(url))
            {
                Debug.WriteLine(
                    $"[DockApp] .url sem chave URL= válida: {path}"
                );

                return null;
            }

            // Tenta converter o índice do ícone de texto para inteiro.
            //
            // Se não for possível converter, iconIndex receberá 0.
            int.TryParse(iconIndexStr, out var iconIndex);

            // Cria o DockItem correspondente ao atalho de Internet.
            return new DockItem
            {
                // Nome do arquivo sem a extensão .url.
                Name = Path.GetFileNameWithoutExtension(path),

                // Caminho original do arquivo .url.
                SourcePath = path,

                // URL que será utilizada como destino do item.
                //
                // Pode ser uma URL tradicional ou outro protocolo
                // reconhecido pelo Windows.
                TargetPath = url,

                // Tenta extrair o ícone especificado pelo arquivo .url.
                Icon = ExtractIcon(iconFile, iconIndex)
            };
        }
        catch (Exception ex)
        {
            // Erros de leitura são tratados individualmente,
            // mantendo o restante dos atalhos funcionando.
            Debug.WriteLine(
                $"[DockApp] Erro ao ler .url '{path}': {ex.Message}"
            );

            return null;
        }
    }

    /// <summary>
    /// Analisa a propriedade IconLocation de um atalho .lnk.
    ///
    /// Normalmente, a localização do ícone possui o formato:
    ///
    /// caminho_do_arquivo,índice
    ///
    /// Exemplo:
    /// C:\Windows\System32\shell32.dll,5
    ///
    /// Caso a localização seja inválida ou esteja vazia,
    /// o caminho de destino do atalho será utilizado como fallback.
    /// </summary>
    /// <param name="iconLocation">
    /// Localização do ícone informada pelo atalho.
    /// </param>
    /// <param name="fallbackPath">
    /// Caminho alternativo utilizado caso IconLocation não seja válido.
    /// </param>
    /// <returns>
    /// Uma tupla contendo o caminho do arquivo do ícone
    /// e seu índice.
    /// </returns>
    private static (string? path, int index) ParseIconLocation(
        string? iconLocation,
        string fallbackPath)
    {
        // Se a localização do ícone estiver vazia,
        // utiliza o próprio destino do atalho como fallback.
        if (string.IsNullOrWhiteSpace(iconLocation))
            return (fallbackPath, 0);

        // Divide a localização utilizando a vírgula.
        //
        // Exemplo:
        // "C:\Windows\System32\shell32.dll,5"
        //
        // Resultado:
        // parts[0] = caminho
        // parts[1] = índice
        var parts = iconLocation.Split(',');

        // Obtém o caminho do arquivo do ícone.
        //
        // Caso a primeira parte esteja vazia, utiliza o fallback.
        var path = string.IsNullOrWhiteSpace(parts[0])
            ? fallbackPath
            : parts[0];

        // Tenta converter a segunda parte para o índice do ícone.
        //
        // Se não existir ou não for um número válido,
        // utiliza o índice 0.
        var index = parts.Length > 1 &&
                    int.TryParse(parts[1], out var i)
            ? i
            : 0;

        return (path, index);
    }

    /// <summary>
    /// Extrai um ícone de um arquivo utilizando a API nativa
    /// <c>ExtractIconEx</c> do Windows.
    ///
    /// O método obtém o identificador nativo do ícone (HICON),
    /// converte esse identificador para um <see cref="ImageSource"/>
    /// compatível com WPF e libera os recursos nativos posteriormente.
    /// </summary>
    /// <param name="path">
    /// Caminho do arquivo que contém o ícone.
    /// Pode ser um executável, DLL ou outro arquivo compatível.
    /// </param>
    /// <param name="index">
    /// Índice do ícone dentro do arquivo.
    /// </param>
    /// <returns>
    /// Um <see cref="ImageSource"/> contendo o ícone extraído,
    /// ou <c>null</c> caso não seja possível extrair o ícone.
    /// </returns>
    private static ImageSource? ExtractIcon(string? path, int index)
    {
        // Verifica se o caminho é válido e se o arquivo existe.
        if (string.IsNullOrWhiteSpace(path) ||
            !System.IO.File.Exists(path))
        {
            return null;
        }

        // Arrays que receberão os identificadores dos ícones
        // grandes e pequenos retornados pelo Windows.
        //
        // O tamanho 1 indica que queremos extrair apenas um ícone.
        var large = new IntPtr[1];
        var small = new IntPtr[1];

        try
        {
            // Solicita ao Windows a extração do ícone.
            //
            // large receberá o HICON do ícone em tamanho grande.
            // small receberá o HICON do ícone em tamanho pequeno.
            var extracted = NativeIcons.ExtractIconEx(
                path,
                index,
                large,
                small,
                1
            );

            // Verifica se algum ícone foi extraído e se o identificador
            // do ícone grande é válido.
            if (extracted <= 0 || large[0] == IntPtr.Zero)
                return null;

            // Converte o HICON nativo para um BitmapSource compatível
            // com o sistema de imagens do WPF.
            var source = Imaging.CreateBitmapSourceFromHIcon(
                large[0],
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            // Torna a imagem imutável e independente da thread
            // que realizou sua criação.
            //
            // Isso permite que o ImageSource seja utilizado com
            // segurança posteriormente pela interface WPF.
            source.Freeze();

            return source;
        }
        catch
        {
            // Qualquer falha na extração do ícone resulta em null.
            //
            // O erro não é propagado porque a ausência de um ícone
            // não deve impedir o carregamento do DockItem.
            return null;
        }
        finally
        {
            // Libera o HICON grande criado pelo Windows.
            //
            // É importante liberar esse recurso nativo para evitar
            // vazamentos de memória/handles.
            if (large[0] != IntPtr.Zero)
                NativeIcons.DestroyIcon(large[0]);

            // Libera também o HICON pequeno, caso tenha sido criado.
            if (small[0] != IntPtr.Zero)
                NativeIcons.DestroyIcon(small[0]);
        }
    }
}

/// <summary>
/// Contém declarações das funções nativas do Windows utilizadas
/// pelo <see cref="ShortcutLoader"/> para trabalhar com ícones.
///
/// As funções são importadas diretamente das DLLs do Windows
/// através de P/Invoke (Platform Invocation Services).
/// </summary>
internal static class NativeIcons
{
    /// <summary>
    /// Extrai um ou mais ícones de um arquivo executável,
    /// DLL ou outro arquivo que contenha recursos de ícones.
    ///
    /// Função nativa:
    /// shell32.dll → ExtractIconEx
    /// </summary>
    /// <param name="lpszFile">
    /// Caminho do arquivo que contém os ícones.
    /// </param>
    /// <param name="nIconIndex">
    /// Índice do primeiro ícone que será extraído.
    /// </param>
    /// <param name="phiconLarge">
    /// Array que receberá os identificadores dos ícones grandes.
    /// </param>
    /// <param name="phiconSmall">
    /// Array que receberá os identificadores dos ícones pequenos.
    /// </param>
    /// <param name="nIcons">
    /// Quantidade de ícones que devem ser extraídos.
    /// </param>
    /// <returns>
    /// Quantidade de ícones extraídos com sucesso.
    /// </returns>
    [DllImport(
        "shell32.dll",
        CharSet = CharSet.Unicode
    )]
    public static extern int ExtractIconEx(
        string lpszFile,
        int nIconIndex,
        IntPtr[] phiconLarge,
        IntPtr[] phiconSmall,
        int nIcons
    );

    /// <summary>
    /// Libera um identificador de ícone (HICON) criado pelo Windows.
    ///
    /// Função nativa:
    /// user32.dll → DestroyIcon
    ///
    /// Deve ser chamada depois que o ícone nativo não for mais utilizado,
    /// evitando vazamentos de recursos do sistema.
    /// </summary>
    /// <param name="hIcon">
    /// Identificador do ícone que deverá ser destruído.
    /// </param>
    /// <returns>
    /// <c>true</c> se o ícone foi destruído com sucesso;
    /// caso contrário, <c>false</c>.
    /// </returns>
    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);
}