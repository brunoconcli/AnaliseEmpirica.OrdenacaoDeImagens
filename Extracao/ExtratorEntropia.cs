namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Métrica alternativa: entropia de Shannon do histograma de luma (diversidade tonal), em [0, 8] bits.
/// </summary>
/// <remarks>
/// Mede quantos tons diferentes a imagem tem e quão uniformemente aparecem, mas
/// ignora a posição dos pixels: uma imagem metade preta, metade branca e um
/// xadrez preto e branco têm a mesma entropia (1 bit).
/// </remarks>
public sealed class ExtratorEntropia : IExtratorPropriedade
{
  private const int NiveisDeCinza = 256;

  public Propriedade Propriedade => Propriedade.Entropia;

  public double Extrair(ImagemRgb imagem)
  {
    ArgumentNullException.ThrowIfNull(imagem);

    var histograma = new int[NiveisDeCinza];
    for (int i = 0; i < imagem.TotalPixels; i++)
    {
      var (r, g, b) = imagem.ObterPixel(i);
      int nivel = (int)Math.Round(ConversorCor.Luma(r, g, b));
      histograma[Math.Clamp(nivel, 0, NiveisDeCinza - 1)]++;
    }

    double entropia = 0;
    foreach (int frequencia in histograma)
    {
      if (frequencia == 0)
      {
        continue;
      }

      double probabilidade = (double)frequencia / imagem.TotalPixels;
      entropia -= probabilidade * Math.Log2(probabilidade);
    }

    return entropia;
  }
}
