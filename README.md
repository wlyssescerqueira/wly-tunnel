# WLY Tunnel

Na interface, clique em **Passo a passo** para consultar o guia completo de primeira
configuração, com os comandos na ordem de execução. O guia tem três abas:
**Resumo** ([RESUMO.txt](RESUMO.txt)), **Windows — seu computador**
([GUIA-WINDOWS.txt](GUIA-WINDOWS.txt), onde roda o app) e **Linux — VPS**
([GUIA-LINUX.txt](GUIA-LINUX.txt), feita pelo administrador). Os textos usam
`000.000.000.000` no lugar do IP da VPS.

Abra `app/WlyTunnel.exe`, confira os dados e clique em **Conectar**.
Use `127.0.0.1:15432` no aplicativo que precisa do PostgreSQL.
O usuário, a senha e o nome do banco continuam sendo os de cada projeto.

Escolha o **Tipo de acesso** antes de conectar. Ele preenche usuário e chave SSH:

| Tipo          | Usuário SSH | Chave                  | Para quem                                              |
| ------------- | ----------- | ---------------------- | ------------------------------------------------------ |
| Administrador | `root`      | `~/.ssh/id_rsa`        | Dono do servidor, com acesso completo                  |
| Desenvolvedor | `devbanco`  | `~/.ssh/id_ed25519_do` | Outro dev: somente o túnel até o PostgreSQL, sem shell |

A chave do desenvolvedor só funciona com `devbanco`. Com `root`, o servidor recusa a
chave e o app mostra essa orientação no diagnóstico.

A tela mostra todos os parâmetros e o diagnóstico do OpenSSH. Os valores iniciais
correspondem ao túnel já utilizado neste computador. É necessário ter a chave SSH
e o servidor previamente reconhecido pelo OpenSSH do Windows. Chaves com senha
precisam estar carregadas no ssh-agent, pois o app não solicita senhas.

O **X** envia a janela para a bandeja perto do relógio, mantendo o túnel no estado atual.
Dê dois cliques no ícone para reabrir. O botão direito oferece Abrir, Conectar,
Desconectar e **Sair e desconectar**. Essa última opção encerra o app e seu túnel.
**Desconectar** encerra somente o processo SSH criado pelo app.
Uma porta ocupada oferece encerrar o processo identificado por nome e PID e reiniciar
o túnel aqui. Isso só acontece após confirmar **Sim**; **Não** mantém o processo atual.
Não há início automático
nem reconexão automática. Para retomar após uma queda, clique em Conectar.
O status conectado exige resposta PostgreSQL no destino, sem autenticar ou alterar dados.

As configurações são salvas em `%LOCALAPPDATA%/WlyTunnel/settings.json`.
O conteúdo da chave e senhas de banco não são armazenados pelo app.

Para compilar com o SDK .NET 9:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o app
```

O executável publicado inclui o runtime; o computador de destino precisa de Windows
64 bits e OpenSSH Client instalado.
