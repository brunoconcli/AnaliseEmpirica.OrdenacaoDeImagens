namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

public interface IComando
{
  /// <summary>Nome digitado no terminal (ex.: "extrair").</summary>
  string Nome { get; }

  /// <summary>Descrição de uma linha, mostrada na ajuda geral.</summary>
  string Resumo { get; }

  /// <summary>Texto completo de uso, com todas as opções.</summary>
  string Uso { get; }

  /// <returns>Código de saída do processo: 0 em caso de sucesso.</returns>
  int Executar(ArgumentosLinhaDeComando argumentos);
}
