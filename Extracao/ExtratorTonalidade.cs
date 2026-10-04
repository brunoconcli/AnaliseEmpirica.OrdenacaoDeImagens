namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// B. Tonalidade: média circular do matiz, ponderada pela croma de cada pixel,
/// em [0, 360), ou <see cref="ItemImagem.TonalidadeAcromatica"/> (−1).
/// </summary>
/// <remarks>
/// Cada pixel contribui com um vetor na direção do seu matiz e comprimento igual
/// à sua croma. Pixels cinzas (croma 0) e muito escuros (croma baixa) quase não pesam.
/// A imagem é acromática quando:
/// <list type="bullet">
///   <item>a croma média fica abaixo de τ (imagem em tons de cinza); ou</item>
///   <item>o vetor resultante é curto em relação ao peso total, abaixo de ρ
///   (cores espalhadas pelo círculo, sem tonalidade predominante).</item>
/// </list>
/// </remarks>
public sealed class ExtratorTonalidade : IExtratorPropriedade
{
  private readonly double _limiarCromaAcromatica;
  private readonly double _limiarConcentracaoMatiz;

  public ExtratorTonalidade(double limiarCromaAcromatica, double limiarConcentracaoMatiz)
  {
    _limiarCromaAcromatica = limiarCromaAcromatica;
    _limiarConcentracaoMatiz = limiarConcentracaoMatiz;
  }

  public Propriedade Propriedade => Propriedade.Tonalidade;

  public double Extrair(ImagemRgb imagem)
  {
    ArgumentNullException.ThrowIfNull(imagem);

    double somaCroma = 0;
    double somaX = 0;
    double somaY = 0;

    for (int i = 0; i < imagem.TotalPixels; i++)
    {
      var (r, g, b) = imagem.ObterPixel(i);
      double croma = ConversorCor.Croma(r, g, b);
      if (croma == 0)
      {
        continue;
      }

      double radianos = ConversorCor.Matiz(r, g, b) * Math.PI / 180;
      somaCroma += croma;
      somaX += croma * Math.Cos(radianos);
      somaY += croma * Math.Sin(radianos);
    }

    if (somaCroma == 0 || somaCroma / imagem.TotalPixels < _limiarCromaAcromatica)
    {
      return ItemImagem.TonalidadeAcromatica;
    }

    double concentracao = Math.Sqrt(somaX * somaX + somaY * somaY) / somaCroma;
    if (concentracao < _limiarConcentracaoMatiz)
    {
      return ItemImagem.TonalidadeAcromatica;
    }

    double graus = Math.Atan2(somaY, somaX) * 180 / Math.PI;
    if (graus < 0)
    {
      graus += 360;
    }

    // Arredondamento de ponto flutuante pode produzir exatamente 360.
    return graus >= 360 ? 0 : graus;
  }
}
