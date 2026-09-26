using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TicketLog.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Os eventos devem ser conectados somente depois de todos os controles existirem.
        foreach (var campo in new[] { ValorMaximo, PrecoTicket })
        {
            campo.PreviewTextInput += Campo_PreviewTextInput;
            campo.PreviewKeyDown += Campo_PreviewKeyDown;
            DataObject.AddPastingHandler(campo, Campo_Colando);
            campo.AllowDrop = false;
            campo.TextChanged += Campo_TextChanged;
        }
        Calcular();
    }

    private void Campo_TextChanged(object sender, TextChangedEventArgs e) => Calcular();

    private void Campo_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
        e.Handled = !InsercaoValida((TextBox)sender, e.Text);

    private void Campo_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
            e.Handled = true;
    }

    private void Campo_Colando(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetData(DataFormats.UnicodeText) is not string texto ||
            !InsercaoValida((TextBox)sender, texto))
        {
            e.CancelCommand();
        }
    }

    private static bool InsercaoValida(TextBox campo, string texto) =>
        TextoMonetarioValido(campo.Text.Remove(campo.SelectionStart, campo.SelectionLength)
            .Insert(campo.SelectionStart, texto));

    private static bool TextoMonetarioValido(string texto) =>
        texto.All(c => (c >= '0' && c <= '9') || c == ',') &&
        texto.Count(c => c == ',') <= 1;

    private void Calcular()
    {
        var valorInformado = LerValor(ValorMaximo.Text);
        var precoTicket = LerValor(PrecoTicket.Text);

        if (!valorInformado.HasValue || !precoTicket.HasValue || valorInformado < 0 || precoTicket <= 0)
        {
            Litros.Text = Reais.Text = ValorLitros.Text = TotalCalculado.Text = "—";
            Status.Text = "Informe valores válidos e um preço do Ticket Log maior que zero.";
            Status.Foreground = new SolidColorBrush(Color.FromRgb(255, 208, 168));
            return;
        }

        Status.Foreground = new SolidColorBrush(Color.FromRgb(255, 175, 180));
        var limiteCentavos = (long)Math.Round(valorInformado.Value * 100, MidpointRounding.AwayFromZero);
        var precoTicketCentavos = (long)Math.Round(precoTicket.Value * 100, MidpointRounding.AwayFromZero);
        var quantidadeLitros = limiteCentavos / precoTicketCentavos;
        var valorDosLitros = quantidadeLitros * precoTicketCentavos;
        var saldoDisponivel = limiteCentavos - valorDosLitros;
        var valorAvulso = saldoDisponivel / 10 * 10;
        var total = Math.Min(limiteCentavos, valorDosLitros + valorAvulso);

        Litros.Text = $"{quantidadeLitros} L";
        Reais.Text = FormatarMoeda(valorAvulso);
        ValorLitros.Text = FormatarMoeda(valorDosLitros);
        TotalCalculado.Text = FormatarMoeda(total);
        Status.Text = total == limiteCentavos
            ? "Valor exato, sem ultrapassar o limite."
            : $"Abaixo do limite por {FormatarMoeda(limiteCentavos - total)}.";
    }

    private static decimal? LerValor(string texto)
    {
        return TextoMonetarioValido(texto) &&
            decimal.TryParse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("pt-BR"), out var resultado)
            ? resultado
            : null;
    }

    private static string FormatarMoeda(long centavos) =>
        (centavos / 100m).ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
}
