namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

/// <summary>
/// Matriz de pixels RGB (8 bits por canal), em ordem de linhas.
/// </summary>
/// <remarks>
/// É o único formato que os extratores conhecem. Assim, eles não dependem da
/// biblioteca de imagens (usada apenas pelo <see cref="PreProcessador"/>) e podem
/// ser testados com imagens sintéticas criadas por <see cref="Criar"/>.
/// </remarks>
public sealed class ImagemRgb
{
  private readonly byte[] _dadosRgb;

  /// <param name="dadosRgb">Canais intercalados: R, G, B do pixel 0, depois do pixel 1, ...</param>
  public ImagemRgb(int largura, int altura, byte[] dadosRgb)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(largura);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(altura);
    ArgumentNullException.ThrowIfNull(dadosRgb);

    if (dadosRgb.Length != largura * altura * 3)
    {
      throw new ArgumentException("O tamanho dos dados não corresponde a largura × altura × 3.", nameof(dadosRgb));
    }

    Largura = largura;
    Altura = altura;
    _dadosRgb = dadosRgb;
  }

  public int Largura { get; }

  public int Altura { get; }

  public int TotalPixels => Largura * Altura;

  /// <summary>Pixel pelo índice linear (linha × largura + coluna).</summary>
  public (byte R, byte G, byte B) ObterPixel(int indice)
  {
    int deslocamento = indice * 3;
    return (_dadosRgb[deslocamento], _dadosRgb[deslocamento + 1], _dadosRgb[deslocamento + 2]);
  }

  public (byte R, byte G, byte B) ObterPixel(int x, int y) => ObterPixel(y * Largura + x);

  /// <summary>Luma de todos os pixels, em ordem de linhas.</summary>
  public double[] CalcularLumas()
  {
    var lumas = new double[TotalPixels];
    for (int i = 0; i < lumas.Length; i++)
    {
      var (r, g, b) = ObterPixel(i);
      lumas[i] = ConversorCor.Luma(r, g, b);
    }

    return lumas;
  }

  /// <summary>Cria uma imagem a partir de uma função que define a cor de cada coordenada (x, y).</summary>
  public static ImagemRgb Criar(int largura, int altura, Func<int, int, (byte R, byte G, byte B)> corDoPixel)
  {
    ArgumentNullException.ThrowIfNull(corDoPixel);

    var dados = new byte[largura * altura * 3];
    for (int y = 0; y < altura; y++)
    {
      for (int x = 0; x < largura; x++)
      {
        var (r, g, b) = corDoPixel(x, y);
        int deslocamento = (y * largura + x) * 3;
        dados[deslocamento] = r;
        dados[deslocamento + 1] = g;
        dados[deslocamento + 2] = b;
      }
    }

    return new ImagemRgb(largura, altura, dados);
  }
}
