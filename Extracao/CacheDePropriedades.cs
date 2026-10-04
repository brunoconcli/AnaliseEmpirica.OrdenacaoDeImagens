namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

using System.Globalization;
using System.Text;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Grava e lê as propriedades extraídas em CSV (o features.csv), para que a extração,
/// que é a etapa cara, rode uma única vez por dataset.
/// </summary>
/// <remarks>
/// Formato: separador vírgula, números com ponto decimal (cultura invariante) e
/// precisão total (round-trip). O caminho do arquivo vem sempre entre aspas, com
/// aspas internas duplicadas, pois pode conter vírgulas.
/// </remarks>
public static class CacheDePropriedades
{
  private const string Cabecalho = "CaminhoArquivo,Luminosidade,Tonalidade,Saturacao,Complexidade,Entropia";
  private const int ColunasNumericas = 5;

  public static void Gravar(string caminhoCsv, IEnumerable<ItemImagem> itens)
  {
    ArgumentNullException.ThrowIfNull(itens);

    string? pasta = Path.GetDirectoryName(Path.GetFullPath(caminhoCsv));
    if (pasta is not null)
    {
      Directory.CreateDirectory(pasta);
    }

    using var escritor = new StreamWriter(caminhoCsv, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    escritor.WriteLine(Cabecalho);

    foreach (var item in itens)
    {
      escritor.Write('"');
      escritor.Write(item.CaminhoArquivo.Replace("\"", "\"\""));
      escritor.Write('"');

      foreach (double valor in new[] { item.Luminosidade, item.Tonalidade, item.Saturacao, item.Complexidade, item.Entropia })
      {
        escritor.Write(',');
        escritor.Write(valor.ToString("R", CultureInfo.InvariantCulture));
      }

      escritor.WriteLine();
    }
  }

  public static List<ItemImagem> Ler(string caminhoCsv)
  {
    using var leitor = new StreamReader(caminhoCsv, Encoding.UTF8);

    string? cabecalho = leitor.ReadLine();
    if (cabecalho != Cabecalho)
    {
      throw new InvalidDataException($"Cabeçalho inesperado em '{caminhoCsv}'. Esperado: {Cabecalho}");
    }

    var itens = new List<ItemImagem>();
    int numeroLinha = 1;
    string? linha;

    while ((linha = leitor.ReadLine()) is not null)
    {
      numeroLinha++;
      if (linha.Length == 0)
      {
        continue;
      }

      try
      {
        itens.Add(LerLinha(linha));
      }
      catch (FormatException ex)
      {
        throw new InvalidDataException($"Linha {numeroLinha} inválida em '{caminhoCsv}': {ex.Message}", ex);
      }
    }

    return itens;
  }

  private static ItemImagem LerLinha(string linha)
  {
    if (linha[0] != '"')
    {
      throw new FormatException("o caminho do arquivo deve estar entre aspas.");
    }

    // Procura a aspa que fecha o caminho, pulando as aspas duplicadas ("").
    var caminho = new StringBuilder();
    int posicao = 1;
    while (true)
    {
      if (posicao >= linha.Length)
      {
        throw new FormatException("aspas do caminho não foram fechadas.");
      }

      if (linha[posicao] == '"')
      {
        if (posicao + 1 < linha.Length && linha[posicao + 1] == '"')
        {
          caminho.Append('"');
          posicao += 2;
          continue;
        }

        break;
      }

      caminho.Append(linha[posicao]);
      posicao++;
    }

    string[] campos = linha[(posicao + 1)..].Split(',');
    // O texto após o caminho começa com vírgula, então o primeiro campo é vazio.
    if (campos.Length != ColunasNumericas + 1 || campos[0].Length != 0)
    {
      throw new FormatException($"esperadas {ColunasNumericas} colunas numéricas após o caminho.");
    }

    double Numero(int indice) => double.Parse(campos[indice], NumberStyles.Float, CultureInfo.InvariantCulture);

    return new ItemImagem(caminho.ToString(), Numero(1), Numero(2), Numero(3), Numero(4), Numero(5));
  }
}
