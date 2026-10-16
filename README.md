# WLY Tunnel

Abra `app/WlyTunnel.exe`, confira os dados e clique em **Conectar**.
Use `127.0.0.1:15432` no aplicativo que precisa do PostgreSQL.
O usuário, a senha e o nome do banco continuam sendo os de cada projeto.

A tela mostra todos os parâmetros e o diagnóstico do OpenSSH. Os valores iniciais
correspondem ao túnel já utilizado neste computador. É necessário ter a chave SSH
e o servidor previamente reconhecido pelo OpenSSH do Windows. Chaves com senha
precisam estar carregadas no ssh-agent, pois o app não solicita senhas.

**Desconectar** e fechar a janela encerram somente o processo SSH criado pelo app.
Uma porta ocupada por outro processo gera um aviso. Não há início automático,
bandeja oculta nem reconexão automática. Para retomar após uma queda, clique em Conectar.
O status conectado exige resposta PostgreSQL no destino, sem autenticar ou alterar dados.

As configurações são salvas em `%LOCALAPPDATA%/WlyTunnel/settings.json`.
O conteúdo da chave e senhas de banco não são armazenados pelo app.

Para compilar com o SDK .NET 9:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o app
```

O executável publicado inclui o runtime; o computador de destino precisa de Windows
64 bits e OpenSSH Client instalado.
