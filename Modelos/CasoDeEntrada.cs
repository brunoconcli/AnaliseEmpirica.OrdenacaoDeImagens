namespace AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Arranjo inicial do vetor entregue ao algoritmo, relativo ao critério de ordenação.
/// </summary>
public enum CasoDeEntrada
{
  /// <summary>Permutação aleatória (caso médio).</summary>
  Aleatorio,

  /// <summary>Já ordenado segundo o critério.</summary>
  Crescente,

  /// <summary>Ordenado no sentido inverso ao do critério.</summary>
  Decrescente,

  /// <summary>Ordenado, com uma pequena fração de elementos fora de posição.</summary>
  QuaseOrdenado,

  /// <summary>Mantido na ordem em que as imagens aparecem no arquivo de origem (ex.: ordem temporal das fotos).</summary>
  OrdemOriginal
}
