# Como publicar uma nova versão do PDV

## Pré-requisito (instalar uma única vez)

1. Instale o **GitHub CLI**:
   ```
   winget install --id GitHub.cli
   ```

2. Faça login no GitHub pelo terminal:
   ```
   gh auth login
   ```
   Siga as instruções na tela (browser abre automaticamente).

---

## Publicar uma atualização

Abra o PowerShell na pasta do projeto e rode:

```powershell
.\release.ps1 1.0.1
```

Substitua `1.0.1` pelo número da nova versão.

### O que acontece automaticamente:
1. A versão é atualizada no projeto
2. O executável `PDV_v1.0.1.exe` é gerado na pasta `release/`
3. A release é criada no GitHub e o arquivo é enviado
4. O código é commitado e enviado ao GitHub

---

## O que o cliente vê

Cerca de 1 minuto após a publicação, ao abrir o PDV o cliente
verá uma janela avisando que há uma nova versão disponível.
Ele clica em **"Atualizar agora"** e o sistema se atualiza sozinho.

---

## Numeração de versões

Use o formato `MAIOR.MENOR.CORREÇÃO`:

| Situação                        | Exemplo          |
|---------------------------------|------------------|
| Correção de bug pequeno         | 1.0.0 → **1.0.1** |
| Nova funcionalidade             | 1.0.1 → **1.1.0** |
| Mudança grande no sistema       | 1.1.0 → **2.0.0** |

---

## Onde fica o banco de dados do cliente

O banco **nunca é afetado** pela atualização.
Ele fica em: `C:\Users\{usuario}\AppData\Local\PDV\pdv.db`
