"""Exercise an imported backup in a disposable copy; requires interactive Windows."""
import argparse
import ctypes
import hashlib
import pathlib
import secrets
import shutil
import subprocess
import tempfile
import time
import xml.etree.ElementTree as ET

from pywinauto import Application

ROOT = pathlib.Path(__file__).resolve().parents[1]


def wait_for(predicate, timeout=25):
    limit = time.monotonic() + timeout
    while time.monotonic() < limit:
        try:
            value = predicate()
            if value:
                return value
        except (OSError, ET.ParseError):
            pass
        time.sleep(0.2)
    raise AssertionError('Expected test condition was not reached')


def visible_grid_has_rows(window):
    for control in window.descendants():
        if control.element_info.control_type not in ('Table', 'DataGrid'):
            continue
        if not control.is_visible():
            continue
        try:
            if control.iface_grid.CurrentRowCount > 0:
                return True
        except Exception:
            if control.descendants(control_type='DataItem'):
                return True
    return False


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('data', type=pathlib.Path)
    args = parser.parse_args()
    source = args.data.resolve(strict=True)
    original_hash = hashlib.sha256(source.read_bytes()).digest()
    folder = pathlib.Path(tempfile.mkdtemp(prefix='visep-imported-ui-'))
    data = folder / 'data.xml'
    app = None
    receiver = None
    try:
        shutil.copy2(source, data)
        root = ET.parse(data).getroot()
        assert root.find('./Users/User') is None, 'Use an imported base before bootstrap'
        client = root.find('./Clients/Client')
        assert client is not None, 'Imported client is required'
        account = client.get('Account')
        assert account, 'Imported client account is required'
        assert root.find('./LegacyHistory/Event') is not None, 'Imported history is required'
        assert root.find('./Incidents/Incident') is None, 'Imported base must have no operational incidents'
        password = secrets.token_urlsafe(24)
        receiver = subprocess.Popen(
            [str(ROOT / 'build/Visep.Receiver.exe'), '--console', str(data)],
            creationflags=subprocess.CREATE_NO_WINDOW,
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        app = Application(backend='uia').start('"%s" "%s"' % (ROOT / 'build/Visep.Desktop.exe', data))
        login = app.window(title='VISEP - Autenticacao')
        login.wait('visible', timeout=20)
        for name, value in [('Usuario', 'importtest'), ('Senha', password), ('Confirmacao (primeiro acesso)', password)]:
            login.child_window(auto_id=name, control_type='Edit').set_edit_text(value)
        login.child_window(title='Entrar / Criar primeiro admin', control_type='Button').invoke()
        window = app.window(title='VISEP - importtest (Admin)')
        window.wait('visible', timeout=25)
        password = None
        print('PASS: isolated copy and administrator login', flush=True)

        def field(name, value):
            window.child_window(auto_id=name, control_type='Edit').set_edit_text(value)

        def click(name):
            button = window.child_window(title=name, control_type='Button')
            button.wait('visible enabled', timeout=25)
            if not ctypes.windll.user32.PostMessageW(button.wrapper_object().handle, 0x00F5, 0, 0):
                raise RuntimeError('Could not post test button click')

        def tab(name):
            window.child_window(title=name, control_type='TabItem').select()

        def dismiss():
            dialog = app.window(title='VISEP - importtest (Admin)', top_level_only=False)
            dialog.child_window(title='OK', control_type='Button').wait('visible', timeout=15).invoke()

        tab('Clientes')
        click('Carregar clientes')
        wait_for(lambda: visible_grid_has_rows(window))
        print('PASS: imported clients rendered in UIA grid', flush=True)
        tab('Historico BYKOM')
        click('Carregar historico')
        wait_for(lambda: visible_grid_has_rows(window))
        assert ET.parse(data).find('./Incidents/Incident') is None, 'History must not create incidents'
        print('PASS: imported history rendered separately in UIA grid', flush=True)
        tab('Simulador')
        for name, value in [('Conta cadastrada', account), ('Codigo', '130'), ('Zona', '001'), ('Particao', '01')]:
            field(name, value)
        click('Enviar simulacao')
        dismiss()
        wait_for(lambda: ET.parse(data).find('./Incidents/Incident') is not None)
        tab('Ocorrencias')
        click('Atualizar')
        wait_for(lambda: visible_grid_has_rows(window))
        click('Assumir')
        wait_for(lambda: ET.parse(data).find('./Incidents/Incident').get('Status') == 'InProgress')
        for button, title, text in [('Registrar acao', 'Acao operacional', 'Synthetic test action'),
                                     ('Encerrar', 'Justificativa de encerramento', 'Synthetic test closure')]:
            click(button)
            prompt = app.window(title=title, top_level_only=False)
            prompt.wait('visible', timeout=15)
            prompt.child_window(control_type='Edit').set_edit_text(text)
            prompt.child_window(title='Confirmar', control_type='Button').invoke()
            wait_for(lambda: text in ET.tostring(ET.parse(data).getroot(), encoding='unicode'))
        incident = ET.parse(data).find('./Incidents/Incident')
        assert incident.get('Status') == 'Closed', 'Incident closure failed'
        assert incident.get('Account') == account, 'Imported account association failed'
        assert incident.get('ClientName') == client.get('Name'), 'Imported client association failed'
        assert hashlib.sha256(source.read_bytes()).digest() == original_hash, 'Source base changed'
        print('PASS: simulation, claim, action, closure and source integrity', flush=True)
    finally:
        try:
            if app is not None:
                app.kill()
        finally:
            try:
                if receiver is not None:
                    receiver.terminate()
                    try:
                        receiver.wait(timeout=10)
                    except subprocess.TimeoutExpired:
                        receiver.kill()
                        receiver.wait(timeout=10)
            finally:
                shutil.rmtree(folder)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print('FAIL: imported desktop test (' + type(error).__name__ + ')', flush=True)
        raise SystemExit(1)
