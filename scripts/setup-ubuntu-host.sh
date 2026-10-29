#!/usr/bin/env bash
# Run personally with sudo on the dedicated tfm-iot VM.
# Source: https://docs.docker.com/engine/install/ubuntu/
set -euo pipefail

grant_docker_access=false
if [[ "${1:-}" == "--grant-docker-access" && $# == 1 ]]; then
    grant_docker_access=true
elif [[ $# != 0 ]]; then
    printf 'Usage: sudo bash %s [--grant-docker-access]\n' "$0" >&2
    exit 2
fi

[[ $EUID == 0 ]] || { printf 'Run with sudo.\n' >&2; exit 1; }
[[ $(hostname) == tfm-iot ]] || { printf 'Expected dedicated VM tfm-iot.\n' >&2; exit 1; }
. /etc/os-release
[[ $ID == ubuntu && $VERSION_ID == 24.04 ]] || { printf 'Expected Ubuntu 24.04.\n' >&2; exit 1; }
[[ $(systemd-detect-virt) == kvm ]] || { printf 'Expected a KVM guest.\n' >&2; exit 1; }
id tfm >/dev/null

for package in docker.io docker-compose docker-compose-v2 docker-doc docker-buildx podman-docker containerd runc; do
    if dpkg-query -W -f='${Status}' "$package" 2>/dev/null | grep -qx 'install ok installed'; then
        printf 'Conflicting package %s exists; review it before proceeding.\n' "$package" >&2
        exit 1
    fi
done

export DEBIAN_FRONTEND=noninteractive
apt-get -o DPkg::Lock::Timeout=300 update
apt-get -o DPkg::Lock::Timeout=300 install -y ca-certificates curl git python3 qemu-guest-agent
install -m 0755 -d /etc/apt/keyrings
curl --proto '=https' --tlsv1.2 -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
chmod 0644 /etc/apt/keyrings/docker.asc
cat > /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: ${UBUNTU_CODENAME:-$VERSION_CODENAME}
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF
apt-get -o DPkg::Lock::Timeout=300 update
apt-get -o DPkg::Lock::Timeout=300 install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
systemctl enable --now docker.service
# qemu-guest-agent uses a device-bound static unit on Ubuntu.
systemctl start qemu-guest-agent.service

if [[ $grant_docker_access == true ]]; then
    printf 'Granting tfm Docker access: this grants effective root access inside this VM.\n'
    usermod -aG docker tfm
fi

docker version
docker compose version
docker run --rm hello-world
systemctl is-active docker.service qemu-guest-agent.service
printf 'Setup finished. Reconnect SSH to refresh group membership.\n'
