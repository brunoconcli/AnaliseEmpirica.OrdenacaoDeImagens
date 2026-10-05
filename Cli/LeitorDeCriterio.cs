namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>Comparador do critério escolhido e a forma de exibir o valor que ele compara.</summary>
public sealed record Criterio(IComparer<ItemImagem> Comparador, Func<ItemImagem, string> DescreverValor);

/// <summary>
/// Monta o critério de ordenação a partir das opções --por, --ordem, --largura-faixa,
/// --desempate e --referencia, compartilhadas pelos comandos ordenar e gerar-resultados.
/// </summary>
public static class LeitorDeCriterio
{
  public const double LarguraFaixaPadrao = 30;

  public static IReadOnlyList<string> Opcoes { get; } = ["por", "ordem", "largura-faixa", "desempate", "referencia"];

  public static string Uso => $"""
      Critérios (--por):
        luminosidade | tonalidade | saturacao | complexidade | entropia
        faixa-tonalidade      Faixas de tonalidade e, dentro delas, desempate por outra propriedade
            --largura-faixa <graus>    Largura de cada faixa (padrão: {LarguraFaixaPadrao})
            --desempate <propriedade>  Propriedade de desempate (padrão: luminosidade)
        distancia             Da imagem mais parecida com a referência até a mais diferente
            --referencia <arquivo>     Caminho ou nome de uma imagem presente no CSV
        --ordem asc|desc               Crescente ou decrescente (padrão: asc)
      """;

  /// <param name="porPadrao">Critério usado quando --por não é informado; se nulo, --por é obrigatório.</param>
  public static Criterio Ler(ArgumentosLinhaDeComando argumentos, IReadOnlyList<ItemImagem> itens, string? porPadrao = null)
  {
    string por = ArgumentosLinhaDeComando.Normalizar(
        argumentos.Texto("por") ?? porPadrao ?? throw new ErroDeUsoException("Informe o critério com --por."));
    var ordem = LerOrdem(argumentos.Texto("ordem") ?? "asc");

    switch (por)
    {
      case "faixa-tonalidade":
        {
          double largura = argumentos.Real("largura-faixa", LarguraFaixaPadrao);
          var desempate = LerPropriedade(argumentos.Texto("desempate") ?? "luminosidade");
          var faixas = new FaixaTonalidadeComparer(largura, desempate);

          return new Criterio(
              ComparerFactory.PorFaixaDeTonalidade(largura, desempate, ordem),
              item =>
              {
                int faixa = faixas.ObterFaixa(item);
                string textoFaixa = faixa < 0 ? "acromática" : $"faixa {faixa}";
                return $"{textoFaixa} | {desempate} {item.ObterValor(desempate):F2}";
              });
        }

      case "distancia":
        {
          var referencia = EncontrarReferencia(
              argumentos.Texto("referencia") ?? throw new ErroDeUsoException("O critério distancia precisa de --referencia."),
              itens);

          return new Criterio(
              ComparerFactory.PorDistancia(referencia, ordem: ordem),
              item => $"d = {DistanciaReferencia.Calcular(item, referencia, PesosDistancia.Iguais):F4}");
        }

      default:
        {
          var propriedade = LerPropriedade(por);
          return new Criterio(
              ComparerFactory.PorPropriedade(propriedade, ordem),
              item => propriedade == Propriedade.Tonalidade && item.IsAcromatica
                  ? "acromática"
                  : $"{item.ObterValor(propriedade):F4}");
        }
    }
  }

  private static Propriedade LerPropriedade(string texto)
  {
    string normalizado = ArgumentosLinhaDeComando.Normalizar(texto);
    foreach (var propriedade in Enum.GetValues<Propriedade>())
    {
      if (ArgumentosLinhaDeComando.Normalizar(propriedade.ToString()) == normalizado)
      {
        return propriedade;
      }
    }

    throw new ErroDeUsoException(
        $"Critério desconhecido: '{texto}'. Use luminosidade, tonalidade, saturacao, complexidade, entropia, faixa-tonalidade ou distancia.");
  }

  private static Ordem LerOrdem(string texto) => ArgumentosLinhaDeComando.Normalizar(texto) switch
  {
    "asc" or "crescente" => Ordem.Crescente,
    "desc" or "decrescente" => Ordem.Decrescente,
    _ => throw new ErroDeUsoException($"Ordem desconhecida: '{texto}'. Use asc ou desc."),
  };

  /// <summary>Procura a imagem de referência no CSV pelo caminho completo ou, se não houver, pelo nome do arquivo.</summary>
  private static ItemImagem EncontrarReferencia(string referencia, IReadOnlyList<ItemImagem> itens)
  {
    string caminhoCompleto = Path.GetFullPath(referencia);
    var porCaminho = itens.FirstOrDefault(i =>
        string.Equals(Path.GetFullPath(i.CaminhoArquivo), caminhoCompleto, StringComparison.OrdinalIgnoreCase));
    if (porCaminho is not null)
    {
      return porCaminho;
    }

    var porNome = itens
        .Where(i => string.Equals(Path.GetFileName(i.CaminhoArquivo), referencia, StringComparison.OrdinalIgnoreCase))
        .ToList();

    return porNome.Count switch
    {
      1 => porNome[0],
      0 => throw new ErroDeUsoException($"A imagem de referência '{referencia}' não está no CSV. Ela precisa ter sido extraída junto com as demais."),
      _ => throw new ErroDeUsoException($"Há {porNome.Count} imagens chamadas '{referencia}' no CSV. Informe o caminho completo."),
    };
  }
}
