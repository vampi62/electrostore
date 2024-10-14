import { createRouter, createWebHistory } from 'vue-router';

import { useAuthStore } from '@/stores';
import {HomeView,
        LoginView,
        ResetPasswordView,
        ForgotPasswordView,
        RegisterView,
        UsersView,
        UserView, 
        InventoryView,
        ProjectsView,
        ProjectView,
        CommandsView,
        CommandView,
        CamerasView,
        IAView,
        TagsView,
        StoresView} from '@/views';

export const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    linkActiveClass: 'active',
    routes: [
        { path: '/', component: HomeView },
        { path: '/login', component: LoginView },
        { path: '/reset-password', component: ResetPasswordView },
        { path: '/forgot-password', component: ForgotPasswordView },
        { path: '/register', component: RegisterView },
        { path: '/users', component: UsersView },
        { path: '/users/:id', component: UserView },
        { path: '/inventory', component: InventoryView },
        { path: '/projects', component: ProjectsView },
        { path: '/projects/:id', component: ProjectView },
        { path: '/commands', component: CommandsView },
        { path: '/commands/:id', component: CommandView },
        { path: '/cameras', component: CamerasView },
        { path: '/ia', component: IAView },
        { path: '/tags', component: TagsView },
        { path: '/stores', component: StoresView },
        { path: '/stores/:id', component: StoresView }
    ]
});

router.beforeEach(async (to) => {
    const publicPages = ['/login', '/register', '/forgot-password', '/reset-password'];
    const authRequired = !publicPages.includes(to.path);
    const auth = useAuthStore();

    if (authRequired && !auth.user) {
        auth.returnUrl = to.fullPath;
        return '/login';
    }
});
