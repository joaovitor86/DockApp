using System.Diagnostics;
using Microsoft.Win32;

namespace DockApp.Services;

/// <summary>
/// Gerencia a inicialização automática do DockApp junto com o Windows.
///
/// A configuração é armazenada no registro do usuário atual (HKCU),
/// portanto não é necessário executar o aplicativo com privilégios
/// de administrador.
///
/// A chave utilizada é:
///
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Run
///
/// Quando habilitada, o Windows executará o DockApp automaticamente
/// durante o login do usuário.
/// </summary>
public static class StartupService
{
    /// <summary>
    /// Caminho da chave do Registro responsável pelos aplicativos
    /// que devem ser executados automaticamente durante a inicialização
    /// da sessão do usuário.
    ///
    /// Como o acesso será feito através de Registry.CurrentUser,
    /// essa configuração pertence apenas ao usuário atual.
    /// </summary>
    private const string RunKeyPath =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Nome utilizado pelo DockApp dentro da chave "Run".
    ///
    /// Esse nome funciona como identificador da entrada do aplicativo
    /// no Registro.
    /// </summary>
    private const string AppName = "DockApp";

    /// <summary>
    /// Habilita ou desabilita a inicialização automática do DockApp
    /// junto com o Windows.
    /// </summary>
    /// <param name="enabled">
    /// <c>true</c> para adicionar ou atualizar a entrada de inicialização.
    ///
    /// <c>false</c> para remover a entrada e impedir que o DockApp
    /// seja iniciado automaticamente.
    /// </param>
    public static void SetEnabled(bool enabled)
    {
        // Abre a chave "Run" do usuário atual com permissão de escrita.
        //
        // Caso a chave ainda não exista, CreateSubKey() cria
        // automaticamente a estrutura necessária.
        //
        // HKCU (HKEY_CURRENT_USER) não exige privilégios de administrador
        // para esse tipo de configuração.
        using var key =
            Registry.CurrentUser.OpenSubKey(
                RunKeyPath,
                writable: true
            )
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        // Verifica se o usuário deseja habilitar a inicialização automática.
        if (enabled)
        {
            // Obtém o caminho completo do executável atualmente em execução.
            //
            // Environment.ProcessPath é a opção preferencial.
            // Caso não esteja disponível, utiliza como alternativa
            // o caminho obtido através do processo atual.
            var exePath =
                Environment.ProcessPath
                ?? Process.GetCurrentProcess().MainModule?.FileName;

            // Só cria a entrada no Registro se um caminho válido
            // para o executável tiver sido encontrado.
            if (!string.IsNullOrEmpty(exePath))
            {
                // Grava o caminho do executável na entrada "DockApp".
                //
                // As aspas são importantes porque o caminho do executável
                // pode conter espaços.
                //
                // Exemplo:
                // "C:\Program Files\DockApp\DockApp.exe"
                key?.SetValue(
                    AppName,
                    $"\"{exePath}\""
                );
            }
        }
        else
        {
            // Remove a entrada "DockApp" da chave Run.
            //
            // throwOnMissingValue: false significa que nenhuma exceção
            // será lançada caso a entrada já não exista.
            key?.DeleteValue(
                AppName,
                throwOnMissingValue: false
            );
        }
    }
}