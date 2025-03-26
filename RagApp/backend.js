const express = require('express');
const app = express();
const port = 3000;

app.use(express.json());

app.post('/chatbot', async (req, res) => {
    const userQuestion = req.body.question;
    
    // Ici, tu appelles ton modèle (Azure.AI, Qdrant, etc.) pour obtenir la réponse
    const aiResponse = await getAiResponse(userQuestion);  // Fonction que tu dois définir pour intégrer ton chatbot
    
    res.json({ answer: aiResponse });
});

app.listen(port, () => {
    console.log(`Server running at http://localhost:${port}`);
});

async function getAiResponse(question) {
    // Logique pour appeler ton modèle d'IA et récupérer la réponse
    // Remplacer ceci par ton code actuel de récupération de réponse
    return "Réponse IA simulée à la question: " + question;
}
