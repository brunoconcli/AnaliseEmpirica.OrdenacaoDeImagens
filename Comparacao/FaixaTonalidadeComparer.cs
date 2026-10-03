namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Abordagem lexicográfica: compara primeiro a faixa de tonalidade e, dentro da
/// mesma faixa, desempata por uma segunda propriedade (por padrão, Luminosidade).
/// </summary>
/// <remarks>
/// A tonalidade é quantizada em faixas disjuntas de largura fixa:
/// faixa = ⌊Tonalidade / largura⌋. Imagens acromáticas formam a faixa −1,
/// anterior a todas as demais.
///
/// A quantização é o que garante a transitividade. Comparar com tolerância
/// ("se |H_A − H_B| &lt; ε, desempata") gera ciclos como A &lt; C &lt; B &lt; A.
/// Ver docs/estudo-propps.md, seção 2.2.
/// </remarks>
public sealed class FaixaTonalidadeComparer : ItemImagemComparer
{
  private const int FaixaAcromatica = -1;

  private readonly PropriedadeComparer _desempate;

  public FaixaTonalidadeComparer(double larguraFaixaGraus, Propriedade desempate = Propriedade.Luminosidade)
  {
    if (larguraFaixaGraus <= 0 || larguraFaixaGraus > 360)
    {
      throw new ArgumentOutOfRangeException(
          nameof(larguraFaixaGraus), larguraFaixaGraus, "A largura da faixa deve estar em (0, 360].");
    }

    if (desempate == Propriedade.Tonalidade)
    {
      throw new ArgumentException("O critério de desempate deve ser diferente de Tonalidade.", nameof(desempate));
    }

    LarguraFaixaGraus = larguraFaixaGraus;
    _desempate = new PropriedadeComparer(desempate);
  }

  public double LarguraFaixaGraus { get; }

  public Propriedade Desempate => _desempate.Propriedade;

  public int ObterFaixa(ItemImagem item) =>
      item.IsAcromatica ? FaixaAcromatica : (int)Math.Floor(item.Tonalidade / LarguraFaixaGraus);

  protected override int CompararItens(ItemImagem x, ItemImagem y)
  {
    int porFaixa = ObterFaixa(x).CompareTo(ObterFaixa(y));
    return porFaixa != 0 ? porFaixa : _desempate.Compare(x, y);
  }

  public override string ToString() => $"Tonalidade (faixas de {LarguraFaixaGraus}°) + {Desempate}";
}
