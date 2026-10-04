namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// C. Saturação: croma média de todos os pixels, em [0, 1].
/// </summary>
/// <remarks>
/// A croma é usada no lugar da saturação HSV porque, no HSV, pixels quase pretos
/// podem ter saturação máxima (ex.: RGB(5, 0, 0) tem S = 1), o que faria fotos
/// escuras com ruído parecerem extremamente vivas.
/// </remarks>
public sealed class ExtratorSaturacao : IExtratorPropriedade
{
  public Propriedade Propriedade => Propriedade.Saturacao;

  public double Extrair(ImagemRgb imagem)
  {
    ArgumentNullException.ThrowIfNull(imagem);

    double soma = 0;
    for (int i = 0; i < imagem.TotalPixels; i++)
    {
      var (r, g, b) = imagem.ObterPixel(i);
      soma += ConversorCor.Croma(r, g, b);
    }

    return soma / imagem.TotalPixels;
  }
}
