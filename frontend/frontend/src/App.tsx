import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Navigation from './components/Navigation';
import AuthCallback from './pages/AuthCallback';
import CharacterSelect from './pages/CharacterSelect';
import CharacterProfile from './pages/CharacterProfile';
import Skills from './pages/Skills';
import Assets from './pages/Assets';
import IndustryJobs from './pages/IndustryJobs';
import MarketOrders from './pages/MarketOrders';
import WalletTransactions from './pages/WalletTransactions';
import Blueprints from './pages/Blueprints';
import MarketData from './pages/MarketData';

function App() {
  return (
    <BrowserRouter>
      <Navigation />
      <Routes>
        <Route path="/" element={<AuthCallback />} />
        <Route path="/auth/callback" element={<AuthCallback />} />
        <Route path="/characters" element={<CharacterSelect />} />
        <Route path="/characters/:characterId" element={<CharacterProfile />} />
        <Route path="/skills" element={<Skills />} />
        <Route path="/assets" element={<Assets />} />
        <Route path="/industry" element={<IndustryJobs />} />
        <Route path="/market-orders" element={<MarketOrders />} />
        <Route path="/wallet" element={<WalletTransactions />} />
        <Route path="/blueprints" element={<Blueprints />} />
        <Route path="/market-data" element={<MarketData />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
