using System.IO;

namespace PDV.WPF.Services;

/// <summary>
/// Dados da empresa exibidos no cabeçalho do comprovante de venda (tela e PDF).
///
/// >>> ATENÇÃO: confirme/corrija os campos marcados com "PLACEHOLDER" abaixo —
///     eles não estavam totalmente legíveis na foto enviada. <<<
/// </summary>
public static class CompanyInfo
{
    public const string Name = "TEC HIDRO MATERIAIS PARA CONSTRUÇÃO";

    // PLACEHOLDER: confirme os 2 últimos dígitos do CNPJ.
    public const string Cnpj = "61.711.883/0001-XX";

    public const string Address = "Estrada da Gabiroba, 613";

    public const string Phones = "(11) 98165-4954   /   (11) 99493-0573";

    // PLACEHOLDER: confirme o e-mail completo.
    public const string Email = "techidromateriais@gmail.com";

    /// <summary>
    /// Caminho do arquivo de logo. Basta salvar um PNG nesse local que ele
    /// passa a aparecer automaticamente no comprovante e no PDF — sem recompilar:
    ///   %LocalAppData%\PDV\logo.png
    /// </summary>
    public static string LogoFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PDV", "logo.png");

    public static bool HasLogo => File.Exists(LogoFilePath);
}
