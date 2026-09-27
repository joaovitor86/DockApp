using System.IO;
using System.Text.Json;
using DockApp.Models;

namespace DockApp.Services;

/// <summary>
/// Serviço responsável por carregar e salvar as configurações
/// do DockApp em um arquivo JSON.
///
/// O arquivo de configuração é armazenado em:
/// %AppData%\DockApp\settings.json
///
/// Como a classe é estática, não é necessário criar uma instância
/// de ConfigurationService para utilizá-la.
/// </summary>
public static class ConfigurationService
{
    /// <summary>
    /// Caminho da pasta onde os arquivos de configuração do DockApp
    /// serão armazenados.
    ///
    /// Environment.SpecialFolder.ApplicationData normalmente aponta
    /// para a pasta AppData\Roaming do usuário atual.
    ///
    /// Exemplo:
    /// C:\Users\Usuario\AppData\Roaming\DockApp
    /// </summary>
    private static readonly string AppFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DockApp"
    );

    /// <summary>
    /// Caminho completo do arquivo JSON que contém as configurações
    /// da Dock.
    ///
    /// Exemplo:
    /// C:\Users\Usuario\AppData\Roaming\DockApp\settings.json
    /// </summary>
    private static readonly string FilePath = Path.Combine(
        AppFolder,
        "settings.json"
    );

    /// <summary>
    /// Carrega as configurações salvas do DockApp.
    ///
    /// O método tenta localizar e ler o arquivo settings.json.
    /// Caso o arquivo não exista, esteja corrompido ou não possa
    /// ser lido, uma nova instância de DockSettings é retornada,
    /// fazendo com que o aplicativo utilize os valores padrão.
    /// </summary>
    /// <returns>
    /// Uma instância de <see cref="DockSettings"/> contendo as
    /// configurações salvas ou, caso não seja possível carregá-las,
    /// uma configuração com os valores padrão.
    /// </returns>
    public static DockSettings Load()
    {
        try
        {
            // Verifica se o arquivo de configuração existe antes
            // de tentar realizar a leitura.
            if (File.Exists(FilePath))
            {
                // Lê todo o conteúdo do arquivo JSON para memória.
                var json = File.ReadAllText(FilePath);

                // Converte o JSON para um objeto DockSettings.
                var settings = JsonSerializer.Deserialize<DockSettings>(json);

                // Se a desserialização foi realizada com sucesso,
                // retorna as configurações carregadas.
                if (settings != null)
                    return settings;
            }
        }
        catch
        {
            // Caso ocorra qualquer erro durante a leitura ou
            // desserialização, o aplicativo não será encerrado.
            //
            // Isso pode acontecer, por exemplo, se:
            // - O arquivo estiver corrompido.
            // - O JSON estiver inválido.
            // - O arquivo não puder ser acessado.
            // - Houver algum problema na desserialização.
            //
            // Nesse caso, o método continua até retornar
            // uma nova configuração com os valores padrão.
        }

        // Retorna uma nova configuração padrão quando:
        // - O arquivo não existe;
        // - O arquivo está vazio ou inválido;
        // - Ocorreu algum erro durante o carregamento;
        // - A desserialização retornou null.
        return new DockSettings();
    }

    /// <summary>
    /// Salva as configurações atuais da Dock no arquivo settings.json.
    ///
    /// A pasta de configuração é criada automaticamente caso ainda
    /// não exista.
    /// </summary>
    /// <param name="settings">
    /// Objeto contendo as configurações que serão persistidas.
    /// </param>
    public static void Save(DockSettings settings)
    {
        // Garante que a pasta %AppData%\DockApp exista.
        //
        // Se a pasta já existir, nenhum erro será gerado.
        Directory.CreateDirectory(AppFolder);

        // Converte o objeto DockSettings para JSON.
        //
        // WriteIndented = true faz com que o arquivo seja formatado
        // com indentação e quebras de linha, facilitando sua leitura
        // manual durante desenvolvimento e depuração.
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            }
        );

        // Grava o JSON no arquivo de configuração.
        //
        // Se o arquivo já existir, seu conteúdo será substituído
        // pelas configurações atuais.
        File.WriteAllText(FilePath, json);
    }
}