using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DockApp.Models;
using DockApp.Services;

// Define explicitamente Color como o Color do WPF.
using Color = System.Windows.Media.Color;

// Define explicitamente ColorConverter como o conversor de cores do WPF.
using ColorConverter = System.Windows.Media.ColorConverter;

// Define explicitamente Brushes como os Brushes do WPF.
using Brushes = System.Windows.Media.Brushes;

// Define explicitamente MessageBox como o MessageBox do WPF.
using MessageBox = System.Windows.MessageBox;

namespace DockApp;

/// <summary>
/// Representa a janela de configurações do DockApp.
///
/// Responsabilidades:
/// - Carregar as configurações atuais.
/// - Exibir os valores salvos nos controles da interface.
/// - Permitir selecionar a pasta dos atalhos.
/// - Permitir alterar a cor da Dock.
/// - Permitir alterar a transparência.
/// - Permitir alterar o arredondamento das bordas.
/// - Permitir alterar o tamanho dos ícones.
/// - Permitir alterar a posição da Dock.
/// - Permitir configurar a inicialização com o Windows.
/// - Pré-visualizar alterações de aparência.
/// - Salvar ou cancelar as alterações.
///
/// As configurações são persistidas através do
/// <see cref="ConfigurationService"/>.
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// Instância das configurações atualmente carregadas.
    ///
    /// O objeto é carregado diretamente do ConfigurationService
    /// quando a janela é criada.
    ///
    /// Durante a edição, os valores dos controles são aplicados
    /// diretamente a esta instância e posteriormente salvos.
    /// </summary>
    private readonly DockSettings _settings =
        ConfigurationService.Load();

    /// <summary>
    /// Callback opcional utilizado para pré-visualizar alterações
    /// de aparência na janela principal da Dock.
    ///
    /// Recebe:
    /// - string: cor hexadecimal.
    /// - double: nível de opacidade.
    ///
    /// É nullable porque a janela pode ser utilizada sem
    /// uma janela principal fornecendo esse callback.
    /// </summary>
    private readonly Action<string, double>? _onPreview;

    /// <summary>
    /// Indica se os campos da janela já foram carregados
    /// com os valores das configurações.
    ///
    /// É utilizado principalmente para impedir que eventos
    /// de alteração dos controles sejam tratados como alterações
    /// do usuário durante a inicialização.
    /// </summary>
    private bool _fieldsLoaded;

    /// <summary>
    /// Inicializa uma nova instância da janela de configurações.
    /// </summary>
    /// <param name="onPreview">
    /// Callback opcional chamado quando o usuário altera valores
    /// relacionados à aparência da Dock.
    /// </param>
    public SettingsWindow(
        Action<string, double>? onPreview = null)
    {
        // Inicializa os componentes definidos no SettingsWindow.xaml.
        InitializeComponent();

        // Armazena o callback recebido pela janela principal.
        _onPreview = onPreview;

        // Aguarda o carregamento completo da janela para preencher
        // os controles com os valores salvos.
        Loaded += (_, _) =>
        {
            LoadIntoFields();

            // A partir deste momento, os eventos de alteração
            // podem ser tratados normalmente.
            _fieldsLoaded = true;
        };
    }

    /// <summary>
    /// Carrega as configurações atuais nos controles da interface.
    ///
    /// Cada propriedade de DockSettings é associada ao controle
    /// correspondente da janela.
    /// </summary>
    private void LoadIntoFields()
    {
        // Carrega o caminho da pasta dos atalhos.
        FolderTextBox.Text = _settings.FolderPath;

        // Carrega a cor hexadecimal da Dock.
        ColorTextBox.Text = _settings.BackgroundColor;

        // Carrega o nível de transparência.
        OpacitySlider.Value = _settings.BackgroundOpacity;

        // Carrega o arredondamento das bordas.
        CornerRadiusSlider.Value = _settings.CornerRadius;

        // Carrega o tamanho dos ícones.
        IconSizeSlider.Value = _settings.IconSize;

        // A seleção não pode ser feita diretamente pelo índice,
        // pois os ComboBoxItem utilizam Tag para armazenar
        // o valor correspondente ao enum DockPosition.
        //
        // Procura o item cujo Tag corresponde à posição salva.
        foreach (ComboBoxItem item in PositionComboBox.Items)
        {
            if (item.Tag?.ToString() ==
                _settings.Position.ToString())
            {
                // Seleciona o item correspondente.
                PositionComboBox.SelectedItem = item;

                break;
            }
        }

        // Carrega a configuração de inicialização com o Windows.
        StartWithWindowsCheckBox.IsChecked =
            _settings.StartWithWindows;

        // Atualiza o pequeno indicador visual da cor.
        UpdateColorPreview();
    }

    /// <summary>
    /// Abre o seletor de pastas do Windows para permitir
    /// que o usuário escolha a pasta que contém os atalhos.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento.
    /// </param>
    private void BrowseFolder_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Cria o diálogo nativo de seleção de pasta.
        var dialog =
            new Microsoft.Win32.OpenFolderDialog
            {
                // Texto exibido no topo do diálogo.
                Title = "Selecione a pasta dos atalhos",

                // Tenta iniciar o diálogo na pasta atualmente
                // preenchida no campo de texto.
                InitialDirectory = FolderTextBox.Text
            };

        // Exibe o diálogo.
        //
        // ShowDialog() retorna true quando o usuário confirma
        // a seleção.
        if (dialog.ShowDialog() == true)
        {
            // Atualiza o campo com o caminho selecionado.
            FolderTextBox.Text = dialog.FolderName;
        }
    }

    /// <summary>
    /// Abre o seletor de cores do Windows quando o usuário
    /// clica na área de pré-visualização da cor.
    /// </summary>
    /// <param name="sender">
    /// Objeto que recebeu o clique.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento de mouse.
    /// </param>
    private void ColorPreview_Click(
        object sender,
        MouseButtonEventArgs e)
    {
        // Cria o diálogo de seleção de cores do Windows Forms.
        using var dialog =
            new System.Windows.Forms.ColorDialog
            {
                // Exibe a versão expandida do seletor de cores.
                FullOpen = true,

                // Tenta utilizar a cor atualmente digitada
                // como cor inicial do diálogo.
                //
                // Caso a cor atual seja inválida, utiliza
                // o cinza escuro padrão da Dock.
                Color =
                    TryParseColor(
                        ColorTextBox.Text,
                        out var current)
                        ? System.Drawing.Color.FromArgb(
                            current.R,
                            current.G,
                            current.B)
                        : System.Drawing.Color.FromArgb(
                            0x20,
                            0x20,
                            0x20)
            };

        // Verifica se o usuário confirmou uma nova cor.
        if (dialog.ShowDialog() ==
            System.Windows.Forms.DialogResult.OK)
        {
            // Obtém a cor escolhida.
            var c = dialog.Color;

            // Converte a cor para o formato hexadecimal
            // utilizado pelo DockSettings.
            ColorTextBox.Text =
                $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    /// <summary>
    /// Executado quando o conteúdo do campo de cor é alterado.
    ///
    /// Atualiza a pré-visualização da cor e notifica a janela
    /// principal para atualizar temporariamente a aparência da Dock.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Argumentos relacionados à alteração do texto.
    /// </param>
    private void ColorTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        // Atualiza o pequeno quadrado de pré-visualização.
        UpdateColorPreview();

        // Solicita à janela principal que aplique
        // temporariamente a nova aparência.
        NotifyPreview();
    }

    /// <summary>
    /// Executado quando o valor do Slider de transparência
    /// é alterado.
    ///
    /// A pré-visualização só é disparada depois que os campos
    /// iniciais foram carregados.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Contém o valor anterior e o novo valor da opacidade.
    /// </param>
    private void OpacitySlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        // Durante o carregamento inicial, o Slider também gera
        // eventos ValueChanged.
        //
        // _fieldsLoaded impede que esses eventos sejam tratados
        // como uma alteração feita pelo usuário.
        if (_fieldsLoaded)
            NotifyPreview();
    }

    /// <summary>
    /// Notifica o callback de pré-visualização sobre uma alteração
    /// válida de cor ou opacidade.
    ///
    /// A pré-visualização só é enviada quando a cor hexadecimal
    /// atual pode ser convertida corretamente.
    /// </summary>
    private void NotifyPreview()
    {
        // Verifica se a cor digitada é válida.
        if (TryParseColor(ColorTextBox.Text, out _))
        {
            // Executa o callback, caso tenha sido fornecido.
            //
            // O operador ?. evita uma chamada quando _onPreview
            // não estiver configurado.
            _onPreview?.Invoke(
                ColorTextBox.Text,
                OpacitySlider.Value);
        }
    }

    /// <summary>
    /// Atualiza o quadrado de pré-visualização da cor.
    ///
    /// Quando o valor digitado é válido, o fundo recebe a cor.
    /// Quando é inválido, o fundo fica transparente.
    /// </summary>
    private void UpdateColorPreview()
    {
        // Tenta converter a string hexadecimal em Color.
        ColorPreview.Background =
            TryParseColor(
                ColorTextBox.Text,
                out var color)

                // Cor válida: cria um pincel com a cor escolhida.
                ? new SolidColorBrush(color)

                // Cor inválida: deixa a pré-visualização transparente.
                : Brushes.Transparent;
    }

    /// <summary>
    /// Tenta converter uma string hexadecimal para uma estrutura
    /// <see cref="Color"/> do WPF.
    ///
    /// O método não lança exceção para o código chamador:
    /// erros de conversão são tratados internamente.
    /// </summary>
    /// <param name="hex">
    /// Cor em formato hexadecimal, como "#202020".
    /// </param>
    /// <param name="color">
    /// Recebe a cor convertida quando a conversão é bem-sucedida.
    /// </param>
    /// <returns>
    /// true quando a cor é válida;
    /// false quando não foi possível realizar a conversão.
    /// </returns>
    private static bool TryParseColor(
        string hex,
        out Color color)
    {
        try
        {
            // Tenta converter a string para Color.
            color =
                (Color)ColorConverter.ConvertFromString(hex);

            return true;
        }
        catch
        {
            // Em caso de erro, retorna o valor padrão de Color.
            color = default;

            return false;
        }
    }

    /// <summary>
    /// Salva todas as configurações preenchidas pelo usuário.
    ///
    /// Antes de salvar, valida a cor hexadecimal.
    /// Depois atualiza o objeto DockSettings, persiste os dados
    /// e configura a inicialização automática do Windows.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento.
    /// </param>
    private void Save_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Valida a cor antes de permitir o salvamento.
        if (!TryParseColor(
                ColorTextBox.Text,
                out _))
        {
            // Informa ao usuário que o formato da cor é inválido.
            MessageBox.Show(
                "Cor inválida. Use o formato #RRGGBB.",
                "Configurações",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Interrompe o salvamento.
            return;
        }

        // Atualiza o caminho da pasta dos atalhos.
        _settings.FolderPath =
            FolderTextBox.Text;

        // Atualiza a cor de fundo.
        _settings.BackgroundColor =
            ColorTextBox.Text;

        // Atualiza a transparência.
        _settings.BackgroundOpacity =
            OpacitySlider.Value;

        // Atualiza o arredondamento das bordas.
        _settings.CornerRadius =
            CornerRadiusSlider.Value;

        // Atualiza o tamanho dos ícones.
        _settings.IconSize =
            IconSizeSlider.Value;

        // A posição não é obtida pelo índice do ComboBox.
        //
        // O valor real é armazenado na propriedade Tag de cada
        // ComboBoxItem, permitindo que a interface apresente
        // nomes em português sem alterar o enum interno.
        if (PositionComboBox.SelectedItem
            is ComboBoxItem selectedItem &&

            Enum.TryParse<DockPosition>(
                selectedItem.Tag?.ToString(),
                out var position))
        {
            // Atualiza a posição configurada.
            _settings.Position = position;
        }

        // Converte o estado do CheckBox em bool.
        //
        // IsChecked é nullable, portanto a comparação com true
        // garante um valor booleano.
        _settings.StartWithWindows =
            StartWithWindowsCheckBox.IsChecked == true;

        // Persiste as configurações no arquivo settings.json.
        ConfigurationService.Save(_settings);

        // Atualiza a configuração de inicialização automática
        // no registro do Windows.
        StartupService.SetEnabled(
            _settings.StartWithWindows);

        // Indica para a janela chamadora que o salvamento
        // foi concluído com sucesso.
        DialogResult = true;

        // Fecha a janela de configurações.
        Close();
    }

    /// <summary>
    /// Cancela as alterações realizadas pelo usuário.
    ///
    /// Nenhuma configuração é salva.
    /// A janela principal poderá utilizar DialogResult=false
    /// para restaurar qualquer pré-visualização temporária.
    /// </summary>
    /// <param name="sender">
    /// Objeto que disparou o evento.
    /// </param>
    /// <param name="e">
    /// Argumentos do evento.
    /// </param>
    private void Cancel_Click(
        object sender,
        RoutedEventArgs e)
    {
        // Informa que as alterações não foram salvas.
        DialogResult = false;

        // Fecha a janela.
        Close();
    }
}