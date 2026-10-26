#!/bin/sh
# Shared by both build entry points. Ad-hoc builds require explicit opt-in.
ioi_signing_preflight() {
  IOI_SIGNING_IDENTITY="${IOI_CODESIGN_IDENTITY:-}"
  if [ -z "$IOI_SIGNING_IDENTITY" ]; then
    if [ "${IOI_ALLOW_ADHOC:-0}" = 1 ]; then
      IOI_SIGNING_IDENTITY=-
      printf '%s\n' 'AVISO: compilação ad hoc de desenvolvimento; atualizações podem invalidar permissões.' >&2
      return
    fi
    printf '%s\n' 'Compilação interrompida: informe IOI_CODESIGN_IDENTITY com uma identidade persistente. A assinatura ad hoc anterior invalidava Acessibilidade a cada atualização.' >&2
    return 1
  fi
  case "$IOI_SIGNING_IDENTITY" in *[!0-9A-Fa-f]*) printf '%s\n' 'Use o hash SHA-1 de 40 caracteres da identidade de assinatura.' >&2; return 1;; esac
  [ "${#IOI_SIGNING_IDENTITY}" = 40 ] || return 1
  security find-identity -v -p codesigning | grep -Fi -- "$IOI_SIGNING_IDENTITY" >/dev/null || {
    printf '%s\n' 'Identidade solicitada indisponível no chaveiro; nenhum aplicativo foi instalado.' >&2
    return 1
  }
}
ioi_sign_app() {
  if [ "$IOI_SIGNING_IDENTITY" = - ]; then
    codesign --force --sign - "$1"
  else
    codesign --force --options runtime --timestamp --entitlements "${2:?Informe o arquivo de entitlements}" --sign "$IOI_SIGNING_IDENTITY" "$1"
  fi
  codesign --verify --deep --strict "$1"
  if [ "$IOI_SIGNING_IDENTITY" != - ]; then
    requirement=$(codesign -d -r- "$1" 2>&1)
    if printf '%s' "$requirement" | grep -q 'designated => cdhash'; then
      printf '%s\n' 'Assinatura recusada: identidade ainda depende do hash desta compilação.' >&2
      return 1
    fi
  fi
}
