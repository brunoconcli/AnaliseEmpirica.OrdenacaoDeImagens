namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Distância euclidiana normalizada entre duas imagens (docs/estudo-propps.md, seção 2.3).
/// </summary>
/// <remarks>
/// Cada diferença é levada para [0, 1] pela faixa teórica da propriedade,
/// o que torna a distância independente do dataset:
/// <list type="bullet">
///   <item>ΔL = |L₁ − L₂| / 255</item>
///   <item>ΔH = menor arco entre os matizes / 180°
///   (0 se ambas acromáticas; 1 se apenas uma for)</item>
///   <item>ΔS = |S₁ − S₂| e ΔD = |D₁ − D₂|, já em [0, 1]</item>
/// </list>
/// </remarks>
public static class DistanciaReferencia
{
  public static double Calcular(ItemImagem item, ItemImagem referencia, PesosDistancia pesos) =>
      Math.Sqrt(CalcularAoQuadrado(item, referencia, pesos));

  /// <summary>
  /// Distância ao quadrado. Preserva a ordem da distância e evita a raiz quadrada,
  /// por isso é a forma usada nas comparações.
  /// </summary>
  public static double CalcularAoQuadrado(ItemImagem item, ItemImagem referencia, PesosDistancia pesos)
  {
    double deltaLuminosidade = (item.Luminosidade - referencia.Luminosidade) / 255.0;
    double deltaTonalidade = DiferencaTonalidade(item, referencia);
    double deltaSaturacao = item.Saturacao - referencia.Saturacao;
    double deltaComplexidade = item.Complexidade - referencia.Complexidade;

    return pesos.Luminosidade * deltaLuminosidade * deltaLuminosidade
        + pesos.Tonalidade * deltaTonalidade * deltaTonalidade
        + pesos.Saturacao * deltaSaturacao * deltaSaturacao
        + pesos.Complexidade * deltaComplexidade * deltaComplexidade;
  }

  /// <summary>Diferença circular de matiz, normalizada em [0, 1].</summary>
  public static double DiferencaTonalidade(ItemImagem a, ItemImagem b)
  {
    if (a.IsAcromatica || b.IsAcromatica)
    {
      return a.IsAcromatica == b.IsAcromatica ? 0 : 1;
    }

    double diferenca = Math.Abs(a.Tonalidade - b.Tonalidade);
    return Math.Min(diferenca, 360 - diferenca) / 180.0;
  }
}
