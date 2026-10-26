#!/bin/sh
set -eu
[ "$#" = 2 ] || { echo 'Uso: verify-update.sh NOVO.app INSTALADO.app' >&2; exit 2; }
candidate=$1
installed=$2
for component in ''; do
  codesign --verify --deep --strict "$candidate$component"
  codesign --verify --deep --strict "$installed$component"
  new_requirement=$(codesign -d -r- "$candidate$component" 2>&1 | sed -n 's/^# //; /^designated =>/p')
  old_requirement=$(codesign -d -r- "$installed$component" 2>&1 | sed -n 's/^# //; /^designated =>/p')
  [ -n "$new_requirement" ] && [ -n "$old_requirement" ] || { echo 'Não foi possível identificar as assinaturas.' >&2; exit 1; }
  if printf '%s\n%s\n' "$new_requirement" "$old_requirement" | grep -q cdhash; then
    echo 'Atualização bloqueada: assinatura ad hoc. A migração para identidade estável exige autorização inicial; não substituir automaticamente.' >&2
    exit 1
  fi
  [ "$new_requirement" = "$old_requirement" ] || { echo 'Atualização bloqueada: identidade do aplicativo/componente mudou.' >&2; exit 1; }
done
echo 'PASS: assinaturas válidas e requisitos de identidade preservados no aplicativo e componente. Nenhum arquivo instalado foi alterado.'
