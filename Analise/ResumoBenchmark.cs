namespace AnaliseEmpirica.OrdenacaoDeImagens.Analise;

using System.Globalization;
using System.Text;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>Média e desvio padrão das medidas de um grupo algoritmo × critério × caso × n.</summary>
public sealed record LinhaResumo(
    string Algoritmo,
    string Criterio,
    CasoDeEntrada Caso,
    int TamanhoAmostra,
    int Repeticoes,
    double ComparacoesMedia,
    double ComparacoesDesvio,
    double MovimentacoesMedia,
    double MovimentacoesDesvio,
    double TempoMsMedia,
    double TempoMsDesvio,
    double TempoMsMediana);

/// <summary>
/// Agrupa os resultados brutos e calcula média e desvio padrão de cada medida (e a mediana do tempo)
/// (docs/metodologia.md, seção 5).
/// </summary>
public static class ResumoBenchmark
{
  private const string Cabecalho =
      "Algoritmo,Criterio,Caso,TamanhoAmostra,Repeticoes,"
      + "ComparacoesMedia,ComparacoesDesvio,MovimentacoesMedia,MovimentacoesDesvio,TempoMsMedia,TempoMsDesvio,TempoMsMediana";

  /// <summary>
  /// Ordena os grupos por caso e tamanho; dentro deles, os algoritmos ficam na ordem em que foram executados.
  /// </summary>
  public static List<LinhaResumo> Calcular(IEnumerable<ResultadoExecucao> resultados)
  {
    ArgumentNullException.ThrowIfNull(resultados);

    return resultados
        .GroupBy(r => (r.Algoritmo, r.Criterio, r.Caso, r.TamanhoAmostra))
        .Select(grupo =>
        {
          double[] comparacoes = [.. grupo.Select(r => (double)r.Comparacoes)];
          double[] movimentacoes = [.. grupo.Select(r => (double)r.Movimentacoes)];
          double[] tempos = [.. grupo.Select(r => r.TempoMs)];

          return new LinhaResumo(
              grupo.Key.Algoritmo,
              grupo.Key.Criterio,
              grupo.Key.Caso,
              grupo.Key.TamanhoAmostra,
              grupo.Count(),
              Estatisticas.Media(comparacoes),
              Estatisticas.DesvioPadraoAmostral(comparacoes),
              Estatisticas.Media(movimentacoes),
              Estatisticas.DesvioPadraoAmostral(movimentacoes),
              Estatisticas.Media(tempos),
              Estatisticas.DesvioPadraoAmostral(tempos),
              Estatisticas.Mediana(tempos));
        })
        .OrderBy(l => l.Caso)
        .ThenBy(l => l.TamanhoAmostra)
        .ToList();
  }

  public static void Gravar(string caminhoCsv, IEnumerable<LinhaResumo> linhas)
  {
    ArgumentNullException.ThrowIfNull(linhas);

    var cultura = CultureInfo.InvariantCulture;
    using var escritor = new StreamWriter(caminhoCsv, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    escritor.WriteLine(Cabecalho);

    foreach (var linha in linhas)
    {
      escritor.WriteLine(string.Join(',',
          Texto(linha.Algoritmo),
          Texto(linha.Criterio),
          linha.Caso,
          linha.TamanhoAmostra.ToString(cultura),
          linha.Repeticoes.ToString(cultura),
          linha.ComparacoesMedia.ToString("R", cultura),
          linha.ComparacoesDesvio.ToString("R", cultura),
          linha.MovimentacoesMedia.ToString("R", cultura),
          linha.MovimentacoesDesvio.ToString("R", cultura),
          linha.TempoMsMedia.ToString("R", cultura),
          linha.TempoMsDesvio.ToString("R", cultura),
          linha.TempoMsMediana.ToString("R", cultura)));
    }
  }

  public static List<LinhaResumo> Ler(string caminhoCsv)
  {
    string[] linhas = File.ReadAllLines(caminhoCsv, Encoding.UTF8);
    if (linhas.Length == 0 || linhas[0] != Cabecalho)
    {
      throw new InvalidDataException($"Cabeçalho inesperado em '{caminhoCsv}'. Esperado: {Cabecalho}");
    }

    var cultura = CultureInfo.InvariantCulture;
    var resultado = new List<LinhaResumo>();

    for (int i = 1; i < linhas.Length; i++)
    {
      if (linhas[i].Length == 0)
      {
        continue;
      }

      var campos = SepararCampos(linhas[i]);
      if (campos.Count != 12 || !Enum.TryParse(campos[2], out CasoDeEntrada caso))
      {
        throw new InvalidDataException($"Linha {i + 1} inválida em '{caminhoCsv}'.");
      }

      double Numero(int indice) => double.Parse(campos[indice], NumberStyles.Float, cultura);

      resultado.Add(new LinhaResumo(
          campos[0],
          campos[1],
          caso,
          int.Parse(campos[3], cultura),
          int.Parse(campos[4], cultura),
          Numero(5),
          Numero(6),
          Numero(7),
          Numero(8),
          Numero(9),
          Numero(10),
          Numero(11)));
    }

    return resultado;
  }

  private static string Texto(string valor) => $"\"{valor.Replace("\"", "\"\"")}\"";

  /// <summary>Separa os campos de uma linha CSV, respeitando aspas e aspas duplicadas ("").</summary>
  private static List<string> SepararCampos(string linha)
  {
    var campos = new List<string>();
    var atual = new StringBuilder();
    bool entreAspas = false;

    for (int i = 0; i < linha.Length; i++)
    {
      char c = linha[i];
      if (entreAspas)
      {
        if (c == '"' && i + 1 < linha.Length && linha[i + 1] == '"')
        {
          atual.Append('"');
          i++;
        }
        else if (c == '"')
        {
          entreAspas = false;
        }
        else
        {
          atual.Append(c);
        }
      }
      else if (c == '"')
      {
        entreAspas = true;
      }
      else if (c == ',')
      {
        campos.Add(atual.ToString());
        atual.Clear();
      }
      else
      {
        atual.Append(c);
      }
    }

    campos.Add(atual.ToString());
    return campos;
  }
}
