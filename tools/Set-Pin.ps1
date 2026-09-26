param(
    [string]$Pin
)

$ErrorActionPreference = 'Stop'
$pinAccessPath = Join-Path $PSScriptRoot '..\PinAccess.cs'

function Read-HiddenPin([string]$Message) {
    $securePin = Read-Host $Message -AsSecureString
    try {
        return [System.Net.NetworkCredential]::new('', $securePin).Password
    }
    finally {
        $securePin.Dispose()
    }
}

try {
    $interactive = -not $PSBoundParameters.ContainsKey('Pin')
    if ($interactive) {
        $Pin = Read-HiddenPin 'Novo PIN (6 digitos)'
    }

    if ($Pin -cnotmatch '\A[0-9]{6}\z') {
        throw 'O PIN deve conter exatamente 6 digitos de 0 a 9.'
    }

    if ($interactive) {
        $confirmation = Read-HiddenPin 'Confirme o novo PIN'
        if ($Pin -cne $confirmation) {
            throw 'Os PINs informados nao coincidem. Nenhum arquivo foi alterado.'
        }
    }

    $source = [System.IO.File]::ReadAllText($pinAccessPath)
    $saltPattern = '(\bSalt\s*=\s*Convert\.FromHexString\(")[0-9A-Fa-f]+("\))'
    $hashPattern = '(\bHashEsperado\s*=\s*Convert\.FromHexString\(")[0-9A-Fa-f]+("\))'
    if ([regex]::Matches($source, $saltPattern).Count -ne 1 -or
        [regex]::Matches($source, $hashPattern).Count -ne 1) {
        throw 'Nao foi possivel localizar Salt e HashEsperado em PinAccess.cs. Nenhum arquivo foi alterado.'
    }

    $salt = New-Object byte[] 16
    $random = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $random.GetBytes($salt)
    }
    finally {
        $random.Dispose()
    }

    # Mesmos parametros usados por PinAccess.Validar: PBKDF2-SHA256, 120.000 iteracoes, 32 bytes.
    $pbkdf2 = [System.Security.Cryptography.Rfc2898DeriveBytes]::new(
        $Pin, $salt, 120000, [System.Security.Cryptography.HashAlgorithmName]::SHA256)
    try {
        $hash = $pbkdf2.GetBytes(32)
    }
    finally {
        $pbkdf2.Dispose()
    }

    $saltHex = [System.BitConverter]::ToString($salt).Replace('-', '')
    $hashHex = [System.BitConverter]::ToString($hash).Replace('-', '')
    $source = [regex]::Replace($source, $saltPattern, ('${1}' + $saltHex + '${2}'))
    $source = [regex]::Replace($source, $hashPattern, ('${1}' + $hashHex + '${2}'))
    [System.IO.File]::WriteAllText($pinAccessPath, $source, [System.Text.UTF8Encoding]::new($false))

    Write-Output 'PIN atualizado em PinAccess.cs. Compile ou publique novamente para aplicar ao aplicativo.'
}
finally {
    $Pin = $null
    $confirmation = $null
}
