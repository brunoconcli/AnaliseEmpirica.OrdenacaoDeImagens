namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

/// <summary>
/// Erro causado pela forma como o comando foi chamado (argumento faltando, valor inválido...).
/// A mensagem é mostrada ao usuário junto com o modo de uso, sem pilha de chamadas.
/// </summary>
public sealed class ErroDeUsoException(string mensagem) : Exception(mensagem);
