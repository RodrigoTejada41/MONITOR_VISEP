"""Windows UI integration; requires pywinauto and an interactive desktop."""
import ctypes, pathlib, secrets, subprocess, tempfile, time, xml.etree.ElementTree as ET
from pywinauto import Application
ROOT = pathlib.Path(__file__).resolve().parents[1]

def wait_for(predicate, timeout=20):
    limit = time.monotonic() + timeout
    while time.monotonic() < limit:
        try:
            value = predicate()
            if value:
                return value
        except (OSError, ET.ParseError):
            pass
        time.sleep(.2)
    raise AssertionError('Condition timed out')

def main():
    folder = pathlib.Path(tempfile.mkdtemp(prefix='visep-ui-'))
    data = folder / 'data.xml'
    password = secrets.token_urlsafe(24)
    receiver = subprocess.Popen([str(ROOT/'build/Visep.Receiver.exe'), '--console', str(data)], creationflags=subprocess.CREATE_NO_WINDOW)
    app = None
    window = None
    try:
        app = Application(backend='uia').start('"%s" "%s"' % (ROOT/'build/Visep.Desktop.exe', data))
        login = app.window(title='VISEP - Autenticacao')
        login.wait('visible', timeout=15)
        for name, value in [('Usuario','uiadmin'),('Senha',password),('Confirmacao (primeiro acesso)',password)]:
            login.child_window(auto_id=name, control_type='Edit').set_edit_text(value)
        login.child_window(title='Criar administrador', control_type='Button').invoke()
        window = app.window(title='VISEP - uiadmin (Admin)')
        window.wait('visible', timeout=20)
        def field(name,value): window.child_window(auto_id=name, control_type='Edit').set_edit_text(value)
        def click(name):
            button = window.child_window(title=name,control_type='Button')
            button.wait('visible enabled', timeout=20)
            # Post instead of Invoke: modal dialogs must not block the test thread.
            if not ctypes.windll.user32.PostMessageW(button.wrapper_object().handle, 0x00F5, 0, 0):
                raise RuntimeError('Could not post button click: ' + name)
        def tab(name): window.child_window(title=name,control_type='TabItem').select()
        def dismiss():
            app.window(title='VISEP - uiadmin (Admin)').child_window(title='OK',control_type='Button').wait('visible',timeout=10).invoke()
        tab('Clientes')
        for name,value in [('Nome','Cliente UI'),('Conta','UI001'),('Endereco','Rua Teste'),('Contatos (texto)','Central teste'),('Equipamentos (texto)','Simulador'),('Zonas (texto)','001 Porta')]: field(name,value)
        click('Cadastrar'); dismiss()
        assert ET.parse(data).find('./Clients/Client').get('Account') == 'UI001'
        tab('Simulador')
        for name,value in [('Conta cadastrada','UI001'),('Codigo','130'),('Zona','001'),('Particao','01')]: field(name,value)
        click('Enviar simulacao'); dismiss()
        wait_for(lambda: ET.parse(data).find('./Incidents/Incident') is not None)
        tab('Ocorrencias'); click('Atualizar'); time.sleep(1); click('Assumir')
        wait_for(lambda: ET.parse(data).find('./Incidents/Incident').get('Status') == 'InProgress')
        for button,title,text in [('Registrar acao','Acao operacional','Contato realizado'),('Encerrar','Justificativa de encerramento','Teste encerrado')]:
            click(button)
            prompt = app.window(title=title, top_level_only=False)
            prompt.wait('visible',timeout=10)
            prompt.child_window(control_type='Edit').set_edit_text(text)
            prompt.child_window(title='Confirmar',control_type='Button').invoke()
            wait_for(lambda: text in ET.tostring(ET.parse(data).getroot(),encoding='unicode'))
        assert ET.parse(data).find('./Incidents/Incident').get('Status') == 'Closed'
        window.close()
        app.wait_for_process_exit(timeout=10)
        app = Application(backend='uia').start('"%s" "%s"' % (ROOT/'build/Visep.Desktop.exe', data))
        login = app.window(title='VISEP - Autenticacao')
        login.wait('visible', timeout=15)
        assert not login.child_window(auto_id='Confirmacao (primeiro acesso)', control_type='Edit').exists(timeout=1)
        assert not login.child_window(title='Criar administrador', control_type='Button').exists(timeout=1)
        for name, value in [('Usuario','uiadmin'),('Senha',password)]:
            login.child_window(auto_id=name, control_type='Edit').set_edit_text(value)
        login.child_window(title='Entrar', control_type='Button').invoke()
        window = app.window(title='VISEP - uiadmin (Admin)')
        window.wait('visible', timeout=20)
        displayed_path = window.child_window(auto_id='Base de dados', control_type='Edit').get_value()
        assert pathlib.Path(displayed_path).samefile(data), displayed_path
        assert len(ET.parse(data).findall('./Users/User')) == 1
        assert ET.parse(data).find('./Incidents/Incident').get('Status') == 'Closed'
        window.capture_as_image().save(str(folder / 'completed.png'))
        print('PASS: UI bootstrap, client, simulation, claim, action, close, relogin and persistence; artifacts:', folder)
    except Exception:
        print('Failure artifacts:', folder, flush=True)
        if window is not None:
            try:
                window.capture_as_image().save(str(folder / 'failure.png'))
            except Exception as capture_error:
                print('Screenshot unavailable:', type(capture_error).__name__, flush=True)
        raise
    finally:
        try:
            if app is not None:
                app.kill()
        finally:
            receiver.terminate(); receiver.wait(timeout=10)

if __name__ == '__main__': main()
