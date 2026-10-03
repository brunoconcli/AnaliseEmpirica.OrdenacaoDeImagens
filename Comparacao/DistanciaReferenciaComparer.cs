namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Abordagem CBIR: ordena da imagem mais parecida com a referência até a mais diferente.
/// </summary>
/// <remarks>
/// A distância é calculada a cada comparação, a partir das propriedades já extraídas.
/// O custo continua O(1): algumas subtrações e multiplicações, sem raiz quadrada.
/// Isso é mais barato do que guardar as distâncias em um dicionário e consultá-lo
/// a cada comparação, e mantém <see cref="ItemImagem"/> imutável.
/// </remarks>
public sealed class DistanciaReferenciaComparer : ItemImagemComparer
{
  public DistanciaReferenciaComparer(ItemImagem referencia, PesosDistancia? pesos = null)
  {
    ArgumentNullException.ThrowIfNull(referencia);

    Referencia = referencia;
    Pesos = pesos ?? PesosDistancia.Iguais;
  }

  public ItemImagem Referencia { get; }

  public PesosDistancia Pesos { get; }

  protected override int CompararItens(ItemImagem x, ItemImagem y) =>
      DistanciaReferencia.CalcularAoQuadrado(x, Referencia, Pesos)
          .CompareTo(DistanciaReferencia.CalcularAoQuadrado(y, Referencia, Pesos));

  public override string ToString() => $"Distância a {Path.GetFileName(Referencia.CaminhoArquivo)}";
}
