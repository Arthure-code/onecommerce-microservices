// Le navigateur demande où déposer l'image, l'y écrit lui-même, puis envoie
// le formulaire qui ne porte plus que le nom du dépôt.
(function () {
    const formulaire = document.querySelector('form[data-depot-image]');

    if (!formulaire) {
        return;
    }

    const choix = formulaire.querySelector('input[type="file"][data-image]');
    const nom = formulaire.querySelector('input[data-nom-image]');
    const erreur = formulaire.querySelector('[data-erreur-image]');

    if (!choix || !nom) {
        return;
    }

    let depose = false;

    formulaire.addEventListener('submit', async function (evenement) {
        if (depose || choix.files.length === 0) {
            return;
        }

        evenement.preventDefault();
        erreur.textContent = '';

        const fichier = choix.files[0];

        try {
            const reponse = await fetch(formulaire.dataset.depotImage, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: 'nomPropose=' + encodeURIComponent(fichier.name)
            });

            if (!reponse.ok) {
                throw new Error('lien refusé');
            }

            const lien = await reponse.json();

            const depot = await fetch(lien.url, {
                method: 'PUT',
                headers: {
                    'x-ms-blob-type': 'BlockBlob',
                    'Content-Type': fichier.type || 'application/octet-stream'
                },
                body: fichier
            });

            if (!depot.ok) {
                throw new Error('dépôt refusé');
            }

            nom.value = lien.nom;
            depose = true;
            formulaire.submit();
        } catch {
            erreur.textContent = "Le dépôt de l'image a échoué. Réessayez.";
        }
    });
})();
