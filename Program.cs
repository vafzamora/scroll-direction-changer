namespace ScrollFlip;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Local\ScrollFlip.SingleInstance", out bool isFirstInstance);
        if (!isFirstInstance)
        {
            MessageBox.Show(
                "O ScrollFlip já está em execução (veja o ícone na bandeja).",
                "ScrollFlip",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        try
        {
            Application.Run(new TrayAppContext());
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "ScrollFlip", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}