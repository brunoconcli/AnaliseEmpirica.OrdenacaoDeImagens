namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// D. Complexidade visual: densidade de bordas, em [0, 1].
/// Fração dos pixels cuja magnitude do gradiente de Sobel (sobre a luma) passa do limiar T.
/// </summary>
/// <remarks>
/// O gradiente precisa dos 8 vizinhos de cada pixel, por isso só os pixels internos
/// ((largura − 2) × (altura − 2)) são avaliados, e a fração é calculada sobre eles.
/// A magnitude máxima possível é ~1442 (4 × 255 × √2).
/// </remarks>
public sealed class ExtratorComplexidade : IExtratorPropriedade
{
  private readonly double _limiarBorda;

  public ExtratorComplexidade(double limiarBorda)
  {
    _limiarBorda = limiarBorda;
  }

  public Propriedade Propriedade => Propriedade.Complexidade;

  public double Extrair(ImagemRgb imagem)
  {
    ArgumentNullException.ThrowIfNull(imagem);

    int largura = imagem.Largura;
    int altura = imagem.Altura;
    if (largura < 3 || altura < 3)
    {
      return 0;
    }

    double[] luma = imagem.CalcularLumas();
    double limiarAoQuadrado = _limiarBorda * _limiarBorda;
    int bordas = 0;

    for (int y = 1; y < altura - 1; y++)
    {
      for (int x = 1; x < largura - 1; x++)
      {
        int centro = y * largura + x;
        double superiorEsquerdo = luma[centro - largura - 1];
        double superior = luma[centro - largura];
        double superiorDireito = luma[centro - largura + 1];
        double esquerdo = luma[centro - 1];
        double direito = luma[centro + 1];
        double inferiorEsquerdo = luma[centro + largura - 1];
        double inferior = luma[centro + largura];
        double inferiorDireito = luma[centro + largura + 1];

        double gradienteX = (superiorDireito + 2 * direito + inferiorDireito)
            - (superiorEsquerdo + 2 * esquerdo + inferiorEsquerdo);
        double gradienteY = (inferiorEsquerdo + 2 * inferior + inferiorDireito)
            - (superiorEsquerdo + 2 * superior + superiorDireito);

        // Compara os quadrados para evitar a raiz quadrada em cada pixel.
        if (gradienteX * gradienteX + gradienteY * gradienteY > limiarAoQuadrado)
        {
          bordas++;
        }
      }
    }

    return (double)bordas / ((largura - 2) * (altura - 2));
  }
}
