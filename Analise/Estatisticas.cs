namespace AnaliseEmpirica.OrdenacaoDeImagens.Analise;

public static class Estatisticas
{
  public static double Media(IReadOnlyCollection<double> valores)
  {
    ArgumentNullException.ThrowIfNull(valores);
    if (valores.Count == 0)
    {
      throw new ArgumentException("É preciso ao menos um valor.", nameof(valores));
    }

    return valores.Sum() / valores.Count;
  }

  /// <summary>
  /// Valor central dos dados ordenados (média dos dois centrais, se a quantidade for par).
  /// Diferente da média, não é puxada por picos isolados, como uma interrupção do sistema.
  /// </summary>
  public static double Mediana(IReadOnlyCollection<double> valores)
  {
    ArgumentNullException.ThrowIfNull(valores);
    if (valores.Count == 0)
    {
      throw new ArgumentException("É preciso ao menos um valor.", nameof(valores));
    }

    double[] ordenados = [.. valores.Order()];
    int meio = ordenados.Length / 2;
    return ordenados.Length % 2 == 1 ? ordenados[meio] : (ordenados[meio - 1] + ordenados[meio]) / 2;
  }

  /// <summary>
  /// Desvio padrão amostral (divisor r − 1), pois as repetições são uma amostra
  /// de todas as execuções possíveis. Com um único valor, retorna 0.
  /// </summary>
  public static double DesvioPadraoAmostral(IReadOnlyCollection<double> valores)
  {
    if (valores.Count < 2)
    {
      return 0;
    }

    double media = Media(valores);
    double somaDosQuadrados = valores.Sum(v => (v - media) * (v - media));
    return Math.Sqrt(somaDosQuadrados / (valores.Count - 1));
  }
}
