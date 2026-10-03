namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Operações sobre vetores compartilhadas pelos algoritmos, já instrumentadas.
/// </summary>
internal static class OperacoesVetor
{
    /// <summary>Troca dois elementos de posição. Conta 2 movimentações (duas escritas no vetor).</summary>
    public static void Trocar<T>(T[] vetor, int i, int j, Contadores contadores)
    {
        (vetor[i], vetor[j]) = (vetor[j], vetor[i]);
        contadores.RegistrarMovimentacoes(2);
    }
}
