namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>Resultado da extração de uma pasta: imagens processadas e arquivos que falharam.</summary>
public sealed record ResultadoExtracao(IReadOnlyList<ItemImagem> Itens, IReadOnlyList<FalhaExtracao> Falhas);

/// <summary>Arquivo que não pôde ser processado (corrompido, formato inválido, sem permissão...).</summary>
public sealed record FalhaExtracao(string CaminhoArquivo, string Motivo);
