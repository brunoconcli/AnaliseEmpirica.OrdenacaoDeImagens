namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;
using AnaliseEmpirica.OrdenacaoDeImagens.Extracao;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>Leituras de entrada compartilhadas pelos comandos que trabalham sobre o features.csv.</summary>
public static class LeitorDeFeatures
{
  /// <param name="caminhoCsv">Caminho informado pelo usuário; se nulo, usa o caminho padrão.</param>
  public static ItemImagem[] Ler(string? caminhoCsv)
  {
    string caminho = caminhoCsv ?? Caminhos.Features;
    if (!File.Exists(caminho))
    {
      throw new ErroDeUsoException($"Arquivo não encontrado: '{caminho}'. Rode o comando \"extrair\" antes.");
    }

    ItemImagem[] itens = [.. CacheDePropriedades.Ler(caminho)];
    return itens.Length > 0
        ? itens
        : throw new ErroDeUsoException($"O arquivo '{caminho}' não contém imagens.");
  }

  public static IAlgoritmoOrdenacao ObterAlgoritmo(string nome) =>
      CatalogoDeAlgoritmos.TryObter(nome, out var algoritmo)
          ? algoritmo
          : throw new ErroDeUsoException(
              $"Algoritmo desconhecido: '{nome}'. Use {string.Join(", ", CatalogoDeAlgoritmos.Nomes)} ou todos.");
}
