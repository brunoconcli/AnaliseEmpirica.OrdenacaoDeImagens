namespace AnaliseEmpirica.OrdenacaoDeImagens.Amostragem;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Sorteia amostras do conjunto de imagens e monta os casos de entrada do benchmark.
/// Ver docs/metodologia.md, seção 4.
/// </summary>
public static class GeradorDeAmostras
{
  /// <summary>
  /// Sorteia <paramref name="tamanho"/> posições distintas de [0, <paramref name="total"/>),
  /// em ordem aleatória (Fisher-Yates parcial).
  /// </summary>
  public static int[] SortearIndices(int total, int tamanho, Random aleatorio)
  {
    ArgumentOutOfRangeException.ThrowIfNegative(tamanho);
    ArgumentOutOfRangeException.ThrowIfGreaterThan(tamanho, total);

    int[] indices = new int[total];
    for (int i = 0; i < total; i++)
    {
      indices[i] = i;
    }

    for (int i = 0; i < tamanho; i++)
    {
      int sorteado = aleatorio.Next(i, total);
      (indices[i], indices[sorteado]) = (indices[sorteado], indices[i]);
    }

    return indices[..tamanho];
  }

  /// <summary>
  /// Monta o vetor de entrada de um caso a partir dos índices sorteados.
  /// Os casos ordenados são relativos ao <paramref name="comparador"/> do critério.
  /// </summary>
  /// <param name="fracaoDesordem">Fração máxima de elementos fora de posição no caso quase ordenado.</param>
  /// <param name="aleatorio">Usado apenas para as trocas do caso quase ordenado.</param>
  public static ItemImagem[] MontarCaso(
      IReadOnlyList<ItemImagem> itens,
      int[] indicesSorteados,
      CasoDeEntrada caso,
      IComparer<ItemImagem> comparador,
      double fracaoDesordem,
      Random aleatorio)
  {
    var amostra = indicesSorteados.Select(i => itens[i]);

    // Order/OrderDescending são estáveis: empates mantêm a ordem do sorteio.
    return caso switch
    {
      CasoDeEntrada.Aleatorio => [.. amostra],
      CasoDeEntrada.Crescente => [.. amostra.Order(comparador)],
      CasoDeEntrada.Decrescente => [.. amostra.OrderDescending(comparador)],
      CasoDeEntrada.QuaseOrdenado => Desordenar([.. amostra.Order(comparador)], fracaoDesordem, aleatorio),
      CasoDeEntrada.OrdemOriginal => [.. indicesSorteados.Order().Select(i => itens[i])],
      _ => throw new ArgumentOutOfRangeException(nameof(caso), caso, "Caso de entrada desconhecido."),
    };
  }

  /// <summary>
  /// Faz ⌊fração · n / 2⌉ trocas entre posições sorteadas. Cada troca tira até 2 elementos
  /// do lugar, então no máximo fração · n elementos ficam fora de posição.
  /// </summary>
  private static ItemImagem[] Desordenar(ItemImagem[] ordenado, double fracaoDesordem, Random aleatorio)
  {
    if (ordenado.Length < 2)
    {
      return ordenado;
    }

    int trocas = (int)Math.Round(fracaoDesordem * ordenado.Length / 2);
    for (int t = 0; t < trocas; t++)
    {
      int i = aleatorio.Next(ordenado.Length);
      int j = aleatorio.Next(ordenado.Length - 1);
      if (j >= i)
      {
        j++; // garante j ≠ i sem precisar sortear de novo
      }

      (ordenado[i], ordenado[j]) = (ordenado[j], ordenado[i]);
    }

    return ordenado;
  }
}
