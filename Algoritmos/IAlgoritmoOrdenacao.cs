namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Contrato comum a todos os algoritmos de ordenação.
/// </summary>
/// <remarks>
/// Os algoritmos são genéricos e não conhecem imagens: o critério de ordem
/// (propriedade e sentido crescente/decrescente) vem inteiramente do comparador.
/// O vetor é ordenado no próprio lugar, em ordem não decrescente segundo o comparador.
/// </remarks>
public interface IAlgoritmoOrdenacao
{
    string Nome { get; }

    /// <summary>Indica se elementos equivalentes mantêm sua ordem relativa original.</summary>
    bool Estavel { get; }

    void Ordenar<T>(T[] vetor, IComparer<T> comparador, Contadores contadores);
}
