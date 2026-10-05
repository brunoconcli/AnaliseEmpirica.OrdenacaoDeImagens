namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using SkiaSharp;

/// <summary>
/// Lê uma imagem do disco e a redimensiona para um quadrado de lado fixo.
/// </summary>
/// <remarks>
/// É a única classe que depende da biblioteca de imagens (SkiaSharp, licença MIT).
/// O redimensionamento para tamanho fixo torna as métricas comparáveis entre imagens
/// de resoluções diferentes, o que é essencial para a densidade de bordas.
/// A proporção original não é preservada: as métricas são globais, e a distorção
/// afeta igualmente todas as imagens de mesma proporção.
/// O canal alfa (transparência) é ignorado.
/// </remarks>
public sealed class PreProcessador
{
  public static IReadOnlySet<string> ExtensoesSuportadas { get; } =
      new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" };

  private readonly int _lado;

  public PreProcessador(int lado)
  {
    ArgumentOutOfRangeException.ThrowIfLessThan(lado, 3);

    _lado = lado;
  }

  public static bool IsSuportada(string caminhoArquivo) =>
      ExtensoesSuportadas.Contains(Path.GetExtension(caminhoArquivo));

  public ImagemRgb Carregar(string caminhoArquivo)
  {
    using var original = SKBitmap.Decode(caminhoArquivo)
        ?? throw new InvalidDataException("Formato não reconhecido ou arquivo corrompido.");

    var destino = new SKImageInfo(_lado, _lado, SKColorType.Rgba8888, SKAlphaType.Unpremul);
    // Filtragem linear com mipmaps: na redução forte (ex.: 4000 px → 128 px) cada pixel
    // de destino representa a média da região, sem o serrilhado que criaria bordas falsas.
    var amostragem = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

    using var redimensionada = original.Resize(destino, amostragem)
        ?? throw new InvalidDataException("Não foi possível redimensionar a imagem.");

    SKColor[] pixels = redimensionada.Pixels;
    var dadosRgb = new byte[pixels.Length * 3];
    for (int i = 0; i < pixels.Length; i++)
    {
      dadosRgb[i * 3] = pixels[i].Red;
      dadosRgb[i * 3 + 1] = pixels[i].Green;
      dadosRgb[i * 3 + 2] = pixels[i].Blue;
    }

    return new ImagemRgb(_lado, _lado, dadosRgb);
  }
}
