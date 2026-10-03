namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

/// <summary>
/// Pesos de cada propriedade na distância à imagem de referência.
/// Por padrão, todas as propriedades pesam igualmente.
/// </summary>
public sealed record PesosDistancia(
    double Luminosidade = 1,
    double Tonalidade = 1,
    double Saturacao = 1,
    double Complexidade = 1)
{
  public static PesosDistancia Iguais { get; } = new();
}
