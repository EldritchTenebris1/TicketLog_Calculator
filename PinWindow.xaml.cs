using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TicketLog.App;

public partial class PinWindow : Window
{
    private readonly PasswordBox[] _camposPin;
    private bool _atualizandoPin;
    private bool _desbloqueado;

    public PinWindow()
    {
        InitializeComponent();
        _camposPin = [PinDigit1, PinDigit2, PinDigit3, PinDigit4, PinDigit5, PinDigit6];
        foreach (var campo in _camposPin)
        {
            campo.PreviewTextInput += Pin_PreviewTextInput;
            campo.PreviewKeyDown += Pin_PreviewKeyDown;
            campo.PasswordChanged += Pin_PasswordChanged;
            campo.GotKeyboardFocus += (_, _) => campo.SelectAll();
            DataObject.AddPastingHandler(campo, Pin_Colando);
        }
        ContentRendered += (_, _) => FocarCampo(0);
    }

    private void Pin_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = true;
        PreencherPin(Array.IndexOf(_camposPin, (PasswordBox)sender), e.Text);
    }

    private void Pin_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var campo = (PasswordBox)sender;
        var indice = Array.IndexOf(_camposPin, campo);
        if (e.Key == Key.Space)
            e.Handled = true;
        else if (e.Key == Key.Back)
        {
            e.Handled = true;
            if (campo.Password.Length == 0 && indice > 0)
                indice--;
            _camposPin[indice].Clear();
            FocarCampo(indice);
        }
        else if (e.Key is Key.Left or Key.Right)
        {
            e.Handled = true;
            FocarCampo(Math.Clamp(indice + (e.Key == Key.Left ? -1 : 1), 0, _camposPin.Length - 1));
        }
    }

    private void Pin_Colando(object sender, DataObjectPastingEventArgs e)
    {
        e.CancelCommand();
        if (e.DataObject.GetData(DataFormats.UnicodeText) is not string texto)
            return;

        var indice = texto.Length == _camposPin.Length
            ? 0
            : Array.IndexOf(_camposPin, (PasswordBox)sender);
        PreencherPin(indice, texto);
    }

    private static bool SomenteDigitos(string texto) =>
        texto.All(c => c >= '0' && c <= '9');

    private void PreencherPin(int indice, string texto)
    {
        if (_desbloqueado || string.IsNullOrEmpty(texto) || !SomenteDigitos(texto) ||
            texto.Length > _camposPin.Length - indice)
            return;

        _atualizandoPin = true;
        try
        {
            for (var i = 0; i < texto.Length; i++)
                _camposPin[indice + i].Password = texto[i].ToString();
        }
        finally
        {
            _atualizandoPin = false;
        }

        PinStatus.Text = string.Empty;
        if (!VerificarPinCompleto())
            FocarCampo(Math.Min(indice + texto.Length, _camposPin.Length - 1));
    }

    private void Pin_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_atualizandoPin || _desbloqueado)
            return;

        PinStatus.Text = string.Empty;
        if (VerificarPinCompleto())
            return;

        var campo = (PasswordBox)sender;
        if (campo.Password.Length == 1)
            FocarCampo(Math.Min(Array.IndexOf(_camposPin, campo) + 1, _camposPin.Length - 1));
    }

    private bool VerificarPinCompleto()
    {
        if (_desbloqueado)
            return true;
        if (_camposPin.Any(campo => campo.Password.Length != 1))
            return false;

        var pin = string.Concat(_camposPin.Select(campo => campo.Password));
        if (!PinAccess.Validar(pin))
        {
            LimparPin();
            PinStatus.Text = "PIN incorreto. Tente novamente.";
            FocarCampo(0);
            return true;
        }

        _desbloqueado = true;
        LimparPin();
        DialogResult = true;
        return true;
    }

    private void LimparPin()
    {
        _atualizandoPin = true;
        try
        {
            foreach (var campo in _camposPin)
                campo.Clear();
        }
        finally
        {
            _atualizandoPin = false;
        }
    }

    private void FocarCampo(int indice)
    {
        _camposPin[indice].Focus();
        _camposPin[indice].SelectAll();
    }

    private void Sair_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
