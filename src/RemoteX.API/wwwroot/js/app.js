// ============================================================
// REMOTEX - SignalR SSH Client
// ============================================================

let connection = null;
let isConnectedToSSH = false;
let commandHistory = [];
let historyIndex = -1;

const terminalOutput = document.getElementById('terminal-output');
const terminalInput = document.getElementById('terminal-input');
const sshConnectBtn = document.getElementById('ssh-connect-btn');
const sshModal = document.getElementById('ssh-modal');
const sshForm = document.getElementById('ssh-form');
const modalCancel = document.getElementById('modal-cancel');

// ============================================================
// INICIALIZACIÓN
// ============================================================

async function initialize() {
    printLine('RemoteX Terminal v1.0.0', 'success');
    printLine('Type "help" for available commands', 'info');
    printLine('Click "SSH Connect" to connect to a remote server', 'info');
    printLine('');

    // Inicializar SignalR
    await initializeSignalR();

    // Event Listeners
    terminalInput.addEventListener('keydown', handleKeyDown);
    sshConnectBtn.addEventListener('click', showSSHModal);
    modalCancel.addEventListener('click', hideSSHModal);
    sshForm.addEventListener('submit', handleSSHConnect);
}

// ============================================================
// SIGNALR CONNECTION
// ============================================================

async function initializeSignalR() {
    try {
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/ssh')
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Event Handlers del servidor
        connection.on('Connected', handleConnected);
        connection.on('Output', handleOutput);
        connection.on('Error', handleError);
        connection.on('Disconnected', handleDisconnected);

        // Conectar
        await connection.start();
        printLine('✓ SignalR connected', 'success');
    } catch (error) {
        printLine(`✗ SignalR connection failed: ${error.message}`, 'error');
    }
}

// ============================================================
// SSH MODAL
// ============================================================

function showSSHModal() {
    sshModal.classList.remove('hidden');
    document.getElementById('ssh-host').focus();
}

function hideSSHModal() {
    sshModal.classList.add('hidden');
    sshForm.reset();
}

async function handleSSHConnect(e) {
    e.preventDefault();

    const config = {
        host: document.getElementById('ssh-host').value,
        port: parseInt(document.getElementById('ssh-port').value),
        username: document.getElementById('ssh-username').value,
        password: document.getElementById('ssh-password').value,
        timeout: 20000,
        terminalType: 'xterm-256color',
        columns: 80,
        rows: 24
    };

    try {
        printLine(`Connecting to ${config.username}@${config.host}:${config.port}...`, 'info');
        await connection.invoke('ConnectToServer', config);
        hideSSHModal();
    } catch (error) {
        printLine(`Connection failed: ${error.message}`, 'error');
    }
}

// ============================================================
// SIGNALR EVENT HANDLERS
// ============================================================

function handleConnected(data) {
    isConnectedToSSH = true;
    printLine(`✓ ${data.message}`, 'success');
    printLine(`Session ID: ${data.sessionId}`, 'info');
    updatePrompt(true);
    sshConnectBtn.textContent = 'Disconnect';
    sshConnectBtn.onclick = disconnectSSH;
}

function handleOutput(data) {
    // Output del servidor SSH
    const lines = data.split('\n');
    lines.forEach(line => {
        if (line.trim()) {
            printRaw(line);
        }
    });
}

function handleError(data) {
    printLine(`✗ Error: ${data.message}`, 'error');
    if (data.errorType === 'ConnectionFailed') {
        isConnectedToSSH = false;
        updatePrompt(false);
    }
}

function handleDisconnected(data) {
    isConnectedToSSH = false;
    printLine(`✓ ${data.message}`, 'info');
    updatePrompt(false);
    sshConnectBtn.textContent = 'SSH Connect';
    sshConnectBtn.onclick = showSSHModal;
}

// ============================================================
// TERMINAL INPUT
// ============================================================

function handleKeyDown(e) {
    if (e.key === 'Enter') {
        const command = terminalInput.value.trim();
        terminalInput.value = '';

        if (!command) return;

        // Agregar a historial
        commandHistory.push(command);
        historyIndex = commandHistory.length;

        // Mostrar comando
        printLine(`$ ${command}`);

        if (isConnectedToSSH) {
            // Enviar al servidor SSH
            sendToSSH(command + '\n');
        } else {
            // Comandos locales
            handleLocalCommand(command);
        }
    } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        if (historyIndex > 0) {
            historyIndex--;
            terminalInput.value = commandHistory[historyIndex];
        }
    } else if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (historyIndex < commandHistory.length - 1) {
            historyIndex++;
            terminalInput.value = commandHistory[historyIndex];
        } else {
            historyIndex = commandHistory.length;
            terminalInput.value = '';
        }
    }
}

async function sendToSSH(input) {
    try {
        await connection.invoke('SendInput', input);
    } catch (error) {
        printLine(`Failed to send input: ${error.message}`, 'error');
    }
}

async function disconnectSSH() {
    try {
        await connection.invoke('Disconnect');
    } catch (error) {
        printLine(`Disconnect failed: ${error.message}`, 'error');
    }
}

// ============================================================
// LOCAL COMMANDS
// ============================================================

function handleLocalCommand(command) {
    const parts = command.split(' ');
    const cmd = parts[0].toLowerCase();

    switch (cmd) {
        case 'help':
            printLine('Available local commands:', 'info');
            printLine('  help       - Show this help');
            printLine('  clear      - Clear terminal');
            printLine('  about      - About RemoteX');
            printLine('  ssh        - Open SSH connection dialog');
            printLine('');
            printLine('Connect to SSH to use remote commands', 'info');
            break;

        case 'clear':
            terminalOutput.innerHTML = '';
            break;

        case 'about':
            printLine('RemoteX - Web-based SSH Terminal', 'success');
            printLine('Built with C# + ASP.NET Core + SignalR', 'info');
            printLine('Version 1.0.0', 'info');
            break;

        case 'ssh':
            showSSHModal();
            break;

        default:
            printLine(`Command not found: ${cmd}`, 'error');
            printLine('Type "help" for available commands or connect to SSH', 'info');
    }
}

// ============================================================
// TERMINAL OUTPUT
// ============================================================

function printLine(text, type = '') {
    const line = document.createElement('div');
    line.className = `output-line ${type}`;
    line.textContent = text;
    terminalOutput.appendChild(line);
    terminalOutput.scrollTop = terminalOutput.scrollHeight;
}

function printRaw(text) {
    const line = document.createElement('div');
    line.className = 'output-line';
    line.textContent = text;
    terminalOutput.appendChild(line);
    terminalOutput.scrollTop = terminalOutput.scrollHeight;
}

function updatePrompt(sshConnected) {
    const promptSpan = document.querySelector('.prompt');
    if (sshConnected) {
        promptSpan.textContent = 'ssh@remote:~$';
        promptSpan.style.color = '#7dd8c5';
    } else {
        promptSpan.textContent = 'remotex@local:~$';
        promptSpan.style.color = '#6fc3df';
    }
}

// ============================================================
// STARTUP
// ============================================================

document.addEventListener('DOMContentLoaded', () => {
    initialize();
});