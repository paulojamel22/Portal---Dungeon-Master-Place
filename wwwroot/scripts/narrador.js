const NarradorDMPlace = {
    synth: window.speechSynthesis,
    utterance: null,
    vozes: [],

    // Inicializa as vozes (o navegador demora um pouco para carregar)
    init: function() {
        this.vozes = this.synth.getVoices();
        if (this.vozes.length === 0) {
            this.synth.onvoiceschanged = () => {
                this.vozes = this.synth.getVoices();
                console.log("Vozes de Aetheria carregadas:", this.vozes.length);
            };
        }
    },

    falar: function(textoId, nomeVozDesejada = null) {
        const elemento = document.getElementById(textoId);
        if (!elemento) return;

        const texto = elemento.innerText;
        this.synth.cancel(); // Para qualquer fala anterior

        this.utterance = new SpeechSynthesisUtterance(texto);

        // --- RITUAL DE ESCOLHA DE VOZ ---
        let vozSelecionada = null;

        if (nomeVozDesejada) {
            // Tenta buscar a voz específica que você pediu
            vozSelecionada = this.vozes.find(v => v.name.includes(nomeVozDesejada));
        }

        if (!vozSelecionada) {
            // Se não achou a específica, busca a melhor "Neural" em PT-BR (Edge/Chrome)
            vozSelecionada = this.vozes.find(v => v.lang.includes('pt-BR') && v.name.includes('Neural')) ||
                             this.vozes.find(v => v.lang.includes('pt-BR') && v.name.includes('Google')) ||
                             this.vozes.find(v => v.lang.includes('pt-BR'));
        }

        if (vozSelecionada) {
            this.utterance.voice = vozSelecionada;
            console.log("Oráculo usando a voz:", vozSelecionada.name);
        }

        // Configurações de Entonação
        this.utterance.rate = 0.9;  // Velocidade (0.1 a 10)
        this.utterance.pitch = 0.9; // Tom (0 a 2)
        this.utterance.volume = 1;  // Volume (0 a 1)

        this.synth.speak(this.utterance);
    },

    parar: function() {
        this.synth.cancel();
    }
};

// Inicia o carregamento das vozes assim que o script carregar
NarradorDMPlace.init();