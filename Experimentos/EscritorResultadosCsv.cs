namespace AnaliseEmpirica.OrdenacaoDeImagens.Experimentos;

using System.Globalization;
using System.Text;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Grava os resultados brutos (uma linha por execução) em CSV, linha a linha,
/// para que resultados parciais sobrevivam a uma interrupção.
/// </summary>
/// <remarks>
/// Números com ponto decimal (cultura invariante); textos sempre entre aspas.
/// </remarks>
public sealed class EscritorResultadosCsv : IDisposable
{
  private const string Cabecalho =
      "Algoritmo,Criterio,Caso,TamanhoAmostra,Repeticao,Semente,TempoMs,Comparacoes,Movimentacoes";

  private readonly StreamWriter _escritor;

  public EscritorResultadosCsv(string caminhoCsv)
  {
    string? pasta = Path.GetDirectoryName(Path.GetFullPath(caminhoCsv));
    if (pasta is not null)
    {
      Directory.CreateDirectory(pasta);
    }

    _escritor = new StreamWriter(caminhoCsv, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    _escritor.WriteLine(Cabecalho);
  }

  public void Escrever(ResultadoExecucao resultado)
  {
    var cultura = CultureInfo.InvariantCulture;
    _escritor.WriteLine(string.Join(',',
        Texto(resultado.Algoritmo),
        Texto(resultado.Criterio),
        resultado.Caso,
        resultado.TamanhoAmostra.ToString(cultura),
        resultado.Repeticao.ToString(cultura),
        resultado.Semente.ToString(cultura),
        resultado.TempoMs.ToString("R", cultura),
        resultado.Comparacoes.ToString(cultura),
        resultado.Movimentacoes.ToString(cultura)));
    _escritor.Flush();
  }

  public void Dispose() => _escritor.Dispose();

  internal static string Texto(string valor) => $"\"{valor.Replace("\"", "\"\"")}\"";
}
