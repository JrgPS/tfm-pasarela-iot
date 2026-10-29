"""Export selected fictitious client identities and the public TLS certificate.

Run with Python 3 on the server. Server credentials are never copied or printed.
"""
import argparse
import json
import os
from pathlib import Path


def main():
    root = Path(__file__).resolve().parents[1]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--count', type=int, default=1)
    parser.add_argument('--secrets', type=Path, default=root / 'secrets')
    parser.add_argument('--output', type=Path, default=root / 'evaluation-client')
    args = parser.parse_args()
    if not 1 <= args.count <= 1000:
        parser.error('--count must be between 1 and 1000')
    source = args.secrets.resolve()
    output = args.output.resolve()
    if output == source or output in source.parents or source in output.parents:
        parser.error('output and server secrets must be separate directories')
    if output.exists() and any(output.iterdir()):
        parser.error('output must be absent or empty; existing files are preserved')
    devices = json.loads((source / 'devices.json').read_text(encoding='utf-8'))
    if not isinstance(devices, list) or len(devices) < args.count:
        parser.error('not enough provisioned devices')
    certificate = (source / 'server.cer').read_bytes()
    if not certificate:
        parser.error('public certificate is empty')
    fields = ('Manufacturer', 'Model', 'Serial', 'Merchant', 'Key')
    selected = []
    for device in devices[:args.count]:
        if not isinstance(device, dict) or any(not isinstance(device.get(k), str) for k in fields):
            parser.error('invalid provisioned device manifest')
        selected.append({key: device[key] for key in fields})
    output.mkdir(parents=True, mode=0o700, exist_ok=True)
    os.chmod(output, 0o700)
    for name, data in (
        ('devices.json', (json.dumps(selected, indent=2) + '\n').encode()),
        ('server.cer', certificate),
    ):
        with (output / name).open('xb') as handle:
            handle.write(data)
        os.chmod(output / name, 0o600)
    print(f'Exported {len(selected)} fictitious device(s) and the public certificate.')
    print('Keep the client package private. No server secrets were exported.')


if __name__ == '__main__':
    main()
