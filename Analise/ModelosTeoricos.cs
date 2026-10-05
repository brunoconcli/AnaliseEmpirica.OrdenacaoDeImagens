namespace AnaliseEmpirica.OrdenacaoDeImagens.Analise;

/// <summary>Número esperado de comparações de um algoritmo no caso médio (entrada aleatória).</summary>
/// <param name="Formula">Fórmula como texto, para legendas e relatório.</param>
public sealed record ModeloTeorico(string Formula, Func<double, double> Comparacoes);

/// <summary>
/// Fórmulas do caso médio, para comparar com o que foi medido no caso Aleatorio.
/// </summary>
/// <remarks>
/// Fórmulas completas quando conhecidas para a variante implementada; no Heap Sort,
/// apenas o termo dominante, de modo que a razão medido/teoria se aproxima de 1
/// à medida que n cresce (os termos de ordem menor perdem peso).
/// </remarks>
public static class ModelosTeoricos
{
  private static readonly Dictionary<string, ModeloTeorico> _porAlgoritmo = new()
  {
    // Inversões esperadas n(n−1)/4, mais uma comparação final por elemento (n − 1),
    // menos os casos em que o elemento é o menor até ali e o laço termina sem comparar (Hₙ − 1).
    ["Insertion Sort"] = new("n(n−1)/4 + n − Hₙ", n => n * (n - 1) / 4 + n - Harmonico(n)),

    ["Selection Sort"] = new("n(n−1)/2", n => n * (n - 1) / 2),

    // Caso médio do Merge Sort top-down (Knuth): n·log₂n − 1,2645n.
    ["Merge Sort"] = new("n·log₂n − 1,26n", n => n * Math.Log2(n) - 1.2645 * n),

    ["Heap Sort"] = new("2n·log₂n (termo dominante)", n => 2 * n * Math.Log2(n)),

    // Partição de Lomuto com n − 1 comparações: Cₙ = 2(n+1)Hₙ − 4n.
    ["Quick Sort"] = new("2(n+1)Hₙ − 4n", n => 2 * (n + 1) * Harmonico(n) - 4 * n),
  };

  public static bool TryObter(string algoritmo, out ModeloTeorico modelo) =>
      _porAlgoritmo.TryGetValue(algoritmo, out modelo!);

  /// <summary>Número harmônico Hₙ = 1 + 1/2 + ... + 1/n (n é arredondado para inteiro).</summary>
  public static double Harmonico(double n)
  {
    double soma = 0;
    for (int k = 1; k <= (int)Math.Round(n); k++)
    {
      soma += 1.0 / k;
    }

    return soma;
  }
}
