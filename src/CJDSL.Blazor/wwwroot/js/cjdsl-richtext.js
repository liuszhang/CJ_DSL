window.CJDSL = window.CJDSL || {};

window.CJDSL.RichText = {
    init: function (editorId, dotNetHelper) {
        var editor = document.getElementById(editorId);
        if (!editor) return;

        // Set initial placeholder behavior
        editor.addEventListener('focus', function () {
            if (editor.innerText.trim() === '' || editor.innerHTML === '<br>') {
                editor.innerHTML = '';
            }
        });

        editor.addEventListener('blur', function () {
            if (editor.innerText.trim() === '') {
                editor.innerHTML = '';
            }
        });

        // Observe content changes
        var observer = new MutationObserver(function (mutations) {
            if (dotNetHelper) {
                dotNetHelper.invokeMethodAsync('OnContentChanged', editor.innerHTML);
            }
        });

        observer.observe(editor, {
            childList: true,
            subtree: true,
            characterData: true
        });

        // Store reference
        editor._dotNetHelper = dotNetHelper;
        editor._observer = observer;
    },

    setContent: function (editorId, content) {
        var editor = document.getElementById(editorId);
        if (editor) {
            editor.innerHTML = content;
        }
    },

    getContent: function (editorId) {
        var editor = document.getElementById(editorId);
        return editor ? editor.innerHTML : '';
    },

    execCommand: function (editorId, command, value) {
        var editor = document.getElementById(editorId);
        if (editor) {
            editor.focus();
            document.execCommand(command, false, value || null);
        }
    }
};
