namespace BicycleDrive
{
    /// <summary>Параметры подключения к базе данных.</summary>
    public static class AppSettings
    {
        public const string ConnectionString =
            @"Server=DESKTOP-FINIK\SQLEXPRESS;Database=BicycleDrive;" +
            @"Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False";
    }
}
