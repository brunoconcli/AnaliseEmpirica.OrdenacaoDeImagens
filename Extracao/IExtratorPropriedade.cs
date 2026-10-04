namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Reduz uma imagem a um único valor escalar de uma propriedade visual.
/// </summary>
/// <remarks>
/// O valor retornado nunca pode ser <c>NaN</c>, pois ele será usado como chave
/// de ordenação (ver docs/estudo-propps.md).
/// </remarks>
public interface IExtratorPropriedade
{
  Propriedade Propriedade { get; }

  double Extrair(ImagemRgb imagem);
}
