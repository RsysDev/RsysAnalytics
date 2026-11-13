using System.Text.RegularExpressions;

namespace ErpWebAnalytics.Application.Comandos;

/// <summary>
/// Procura instruções que alteram dados ou esquema. Comentários e textos entre aspas são ignorados.
/// Serve para impedir que um passo "que devolve dados" (liberado em executar_comando, somente leitura) escreva no banco.
/// Escrita em tabela temporária (<c>#t</c>, <c>@t</c>, <c>select ... into #t</c>) e chamadas <c>exec</c> não contam: a primeira só mexe em tempdb e a segunda pode ser uma leitura.
/// </summary>
public static class DetectorEscrita
{
    private static readonly Regex Comentarios = new(@"--[^\r\n]*|/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex Textos = new(@"'(?:[^']|'')*'", RegexOptions.Compiled);
    // Escrever em tabela temporária (#t) ou variável de tabela (@t) só mexe em tempdb: não é escrita no banco do ERP.
    private static readonly Regex EmTemporaria = new(
        @"\b(insert\s+into|update|delete(\s+from)?|truncate\s+table|drop\s+table(\s+if\s+exists)?|alter\s+table|create\s+table|merge(\s+into)?)\s+[#@]\w+" +
        @"|\bcreate\s+(unique\s+)?(clustered\s+|nonclustered\s+)?index\s+\w+\s+on\s+#\w+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex Escrita = new(
        @"\b(insert|update|delete|truncate|drop|alter|merge|create|grant|revoke)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Primeira palavra de escrita encontrada, ou null.</summary>
    public static string? Encontrar(string sql)
    {
        var limpo = EmTemporaria.Replace(Textos.Replace(Comentarios.Replace(sql, " "), "''"), " ");
        var m = Escrita.Match(limpo);
        return m.Success ? m.Value.ToLowerInvariant() : null;
    }
}
