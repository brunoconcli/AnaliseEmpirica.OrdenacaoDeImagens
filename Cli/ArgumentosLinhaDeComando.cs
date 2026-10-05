namespace AnaliseEmpirica.OrdenacaoDeImagens.Cli;

using System.Globalization;
using System.Text;

/// <summary>
/// Separa os argumentos em posicionais e opções no formato <c>--nome valor</c>.
/// </summary>
/// <remarks>
/// Uma opção seguida de outra opção (ou no fim da linha) é tratada como sinalizador
/// sem valor. Números aceitam tanto ponto quanto vírgula decimal.
/// </remarks>
public sealed class ArgumentosLinhaDeComando
{
  private const string PrefixoOpcao = "--";

  private readonly List<string> _posicionais = [];
  private readonly Dictionary<string, string?> _opcoes = new(StringComparer.OrdinalIgnoreCase);

  public ArgumentosLinhaDeComando(IEnumerable<string> argumentos)
  {
    var fila = new Queue<string>(argumentos);
    while (fila.Count > 0)
    {
      string atual = fila.Dequeue();
      if (!atual.StartsWith(PrefixoOpcao, StringComparison.Ordinal))
      {
        _posicionais.Add(atual);
        continue;
      }

      string nome = atual[PrefixoOpcao.Length..];
      string? valor = fila.Count > 0 && !fila.Peek().StartsWith(PrefixoOpcao, StringComparison.Ordinal)
          ? fila.Dequeue()
          : null;

      if (!_opcoes.TryAdd(nome, valor))
      {
        throw new ErroDeUsoException($"A opção --{nome} foi informada mais de uma vez.");
      }
    }
  }

  public string? Posicional(int indice) => indice < _posicionais.Count ? _posicionais[indice] : null;

  public string Posicional(int indice, string descricao) =>
      Posicional(indice) ?? throw new ErroDeUsoException($"Informe {descricao}.");

  public bool Tem(string nome) => _opcoes.ContainsKey(nome);

  public string? Texto(string nome)
  {
    if (!_opcoes.TryGetValue(nome, out string? valor))
    {
      return null;
    }

    return valor ?? throw new ErroDeUsoException($"A opção --{nome} precisa de um valor.");
  }

  public int Inteiro(string nome, int padrao)
  {
    string? texto = Texto(nome);
    if (texto is null)
    {
      return padrao;
    }

    return int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor)
        ? valor
        : throw new ErroDeUsoException($"A opção --{nome} espera um número inteiro, mas recebeu '{texto}'.");
  }

  public double Real(string nome, double padrao)
  {
    string? texto = Texto(nome);
    if (texto is null)
    {
      return padrao;
    }

    return double.TryParse(texto.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double valor)
        ? valor
        : throw new ErroDeUsoException($"A opção --{nome} espera um número, mas recebeu '{texto}'.");
  }

  /// <summary>Valores separados por vírgula (ex.: --casos aleatorio,crescente). Nulo se a opção não foi informada.</summary>
  public IReadOnlyList<string>? Lista(string nome)
  {
    string? texto = Texto(nome);
    if (texto is null)
    {
      return null;
    }

    var valores = texto.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return valores.Length > 0
        ? valores
        : throw new ErroDeUsoException($"A opção --{nome} precisa de ao menos um valor.");
  }

  /// <summary>Inteiros separados por vírgula (ex.: --tamanhos 100,500,1000). Nulo se a opção não foi informada.</summary>
  public IReadOnlyList<int>? ListaDeInteiros(string nome) => Lista(nome)?
      .Select(texto => int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out int valor)
          ? valor
          : throw new ErroDeUsoException($"A opção --{nome} espera números inteiros, mas recebeu '{texto}'."))
      .ToList();

  /// <summary>Rejeita opções desconhecidas, para que um erro de digitação não seja ignorado em silêncio.</summary>
  public void ValidarOpcoes(params IEnumerable<string> conhecidas)
  {
    var desconhecidas = _opcoes.Keys.Except(conhecidas, StringComparer.OrdinalIgnoreCase).ToList();
    if (desconhecidas.Count > 0)
    {
      throw new ErroDeUsoException($"Opção desconhecida: {string.Join(", ", desconhecidas.Select(o => PrefixoOpcao + o))}.");
    }
  }

  /// <summary>Remove acentos e padroniza para minúsculas, para aceitar "saturação" e "Saturacao" igualmente.</summary>
  public static string Normalizar(string texto)
  {
    var semAcentos = new StringBuilder(texto.Length);
    foreach (char c in texto.Normalize(NormalizationForm.FormD))
    {
      if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
      {
        semAcentos.Append(c);
      }
    }

    return semAcentos.ToString().ToLowerInvariant();
  }
}
