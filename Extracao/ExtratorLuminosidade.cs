namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// A. Luminosidade: média da luma (BT.601) de todos os pixels, em [0, 255].
/// </summary>
public sealed class ExtratorLuminosidade : IExtratorPropriedade
{
  public Propriedade Propriedade => Propriedade.Luminosidade;

  public double Extrair(ImagemRgb imagem)
  {
    ArgumentNullException.ThrowIfNull(imagem);

    double soma = 0;
    for (int i = 0; i < imagem.TotalPixels; i++)
    {
      var (r, g, b) = imagem.ObterPixel(i);
      soma += ConversorCor.Luma(r, g, b);
    }

    return soma / imagem.TotalPixels;
  }
}
